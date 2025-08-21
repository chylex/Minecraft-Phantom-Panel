using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Data.Web.Minecraft;
using Phantom.Controller.Database;
using Phantom.Controller.Database.Entities;
using Phantom.Controller.Database.Repositories;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Controller.Services.Instances;

sealed class InstanceDatabaseStorageActor : ReceiveActor<InstanceDatabaseStorageActor.ICommand> {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceDatabaseStorageActor>();
	
	public readonly record struct Init(Guid InstanceGuid, IDbContextProvider DbProvider, CancellationToken CancellationToken);
	
	public static Props<ICommand> Factory(Init init) {
		return Props<ICommand>.Create(() => new InstanceDatabaseStorageActor(init), new ActorConfiguration { SupervisorStrategy = SupervisorStrategies.Resume });
	}
	
	private readonly Guid instanceGuid;
	private readonly IDbContextProvider dbProvider;
	private readonly CancellationToken cancellationToken;
	
	private InstanceDatabaseStorageActor(Init init) {
		this.instanceGuid = init.InstanceGuid;
		this.dbProvider = init.DbProvider;
		this.cancellationToken = init.CancellationToken;
		
		ReceiveAsync<StoreInstanceConfigurationCommand>(StoreInstanceConfiguration);
		ReceiveAsync<StoreInstanceLaunchedCommand>(StoreInstanceLaunched);
		ReceiveAsync<StoreInstanceStoppedCommand>(StoreInstanceStopped);
		ReceiveAsync<StoreInstanceCommandSentCommand>(StoreInstanceCommandSent);
	}
	
	private ValueTask<InstanceEntity?> FindInstanceEntity(ILazyDbContext db) {
		return db.Ctx.Instances.FindAsync(new object[] { instanceGuid }, cancellationToken);
	}
	
	public interface ICommand {}
	
	public sealed record StoreInstanceConfigurationCommand(Guid AuditLogUserGuid, bool IsCreatingInstance, InstanceConfiguration Configuration) : ICommand;
	
	public sealed record StoreInstanceLaunchedCommand(Guid AuditLogUserGuid) : ICommand;
	
	public sealed record StoreInstanceStoppedCommand(Guid AuditLogUserGuid, MinecraftStopStrategy StopStrategy) : ICommand;
	
	public sealed record StoreInstanceCommandSentCommand(Guid AuditLogUserGuid, string Command) : ICommand;
	
	private async Task StoreInstanceConfiguration(StoreInstanceConfigurationCommand command) {
		var configuration = command.Configuration;
		
		await using (var db = dbProvider.Lazy()) {
			InstanceEntity entity = db.Ctx.InstanceUpsert.Fetch(instanceGuid);
			entity.AgentGuid = configuration.AgentGuid;
			entity.InstanceName = configuration.InstanceName;
			entity.ServerPort = configuration.ServerPort;
			entity.RconPort = configuration.RconPort;
			entity.MinecraftVersion = configuration.MinecraftVersion;
			entity.MinecraftServerKind = configuration.MinecraftServerKind;
			entity.MemoryAllocation = configuration.MemoryAllocation;
			entity.JavaRuntimeGuid = configuration.JavaRuntimeGuid;
			entity.JvmArguments = JvmArgumentsHelper.Join(configuration.JvmArguments);
			
			var auditLogWriter = new AuditLogRepository(db).Writer(command.AuditLogUserGuid);
			if (command.IsCreatingInstance) {
				auditLogWriter.InstanceCreated(instanceGuid);
			}
			else {
				auditLogWriter.InstanceEdited(instanceGuid);
			}
			
			await db.Ctx.SaveChangesAsync(cancellationToken);
		}
		
		Logger.Information("Stored instance \"{InstanceName}\" (GUID {InstanceGuid}) in database.", configuration.InstanceName, instanceGuid);
	}
	
	private async Task StoreInstanceLaunched(StoreInstanceLaunchedCommand command) {
		await using var db = dbProvider.Lazy();
		
		var entity = await FindInstanceEntity(db);
		if (entity != null) {
			entity.LaunchAutomatically = true;
		}
		
		var auditLogWriter = new AuditLogRepository(db).Writer(command.AuditLogUserGuid);
		auditLogWriter.InstanceLaunched(instanceGuid);
		
		await db.Ctx.SaveChangesAsync(cancellationToken);
	}
	
	private async Task StoreInstanceStopped(StoreInstanceStoppedCommand command) {
		await using var db = dbProvider.Lazy();
		
		var entity = await FindInstanceEntity(db);
		if (entity != null) {
			entity.LaunchAutomatically = false;
		}
		
		var auditLogWriter = new AuditLogRepository(db).Writer(command.AuditLogUserGuid);
		auditLogWriter.InstanceStopped(instanceGuid, command.StopStrategy.Seconds);
		
		await db.Ctx.SaveChangesAsync(cancellationToken);
	}
	
	private async Task StoreInstanceCommandSent(StoreInstanceCommandSentCommand command) {
		await using var db = dbProvider.Lazy();
		
		var auditLogWriter = new AuditLogRepository(db).Writer(command.AuditLogUserGuid);
		auditLogWriter.InstanceCommandExecuted(instanceGuid, command.Command);
		
		await db.Ctx.SaveChangesAsync(cancellationToken);
	}
}
