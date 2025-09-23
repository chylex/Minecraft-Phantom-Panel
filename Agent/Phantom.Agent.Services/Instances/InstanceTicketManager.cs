using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceTicketManager(AgentInfo agentInfo, ControllerConnection controllerConnection) {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceTicketManager>();
	
	private readonly ControllerSendQueue<ReportAgentStatusMessage> reportStatusQueue = new (controllerConnection, nameof(InstanceTicketManager), capacity: 1, singleWriter: true);
	
	private readonly HashSet<Guid> activeTicketGuids = [];
	private readonly HashSet<ushort> usedPorts = [];
	private RamAllocationUnits usedMemory = new ();
	
	public Result<Ticket, LaunchInstanceResult> Reserve(InstanceConfiguration configuration) {
		var memoryAllocation = configuration.MemoryAllocation;
		var serverPort = configuration.ServerPort;
		var rconPort = configuration.RconPort;
		
		if (!agentInfo.AllowedServerPorts.Contains(serverPort)) {
			return LaunchInstanceResult.ServerPortNotAllowed;
		}
		
		if (!agentInfo.AllowedRconPorts.Contains(rconPort)) {
			return LaunchInstanceResult.RconPortNotAllowed;
		}
		
		lock (this) {
			if (activeTicketGuids.Count + 1 > agentInfo.MaxInstances) {
				return LaunchInstanceResult.InstanceLimitExceeded;
			}
			
			if (usedMemory + memoryAllocation > agentInfo.MaxMemory) {
				return LaunchInstanceResult.MemoryLimitExceeded;
			}
			
			if (usedPorts.Contains(serverPort)) {
				return LaunchInstanceResult.ServerPortAlreadyInUse;
			}
			
			if (usedPorts.Contains(rconPort)) {
				return LaunchInstanceResult.RconPortAlreadyInUse;
			}
			
			var ticket = new Ticket(Guid.NewGuid(), memoryAllocation, serverPort, rconPort);
			
			activeTicketGuids.Add(ticket.TicketGuid);
			usedMemory += memoryAllocation;
			usedPorts.Add(serverPort);
			usedPorts.Add(rconPort);
			
			RefreshAgentStatus();
			Logger.Debug("Reserved ticket {TicketGuid} (server port {ServerPort}, rcon port {RconPort}, memory allocation {MemoryAllocation} MB).", ticket.TicketGuid, ticket.ServerPort, ticket.RconPort, ticket.MemoryAllocation.InMegabytes);
			
			return ticket;
		}
	}
	
	public bool IsValid(Ticket ticket) {
		lock (this) {
			return activeTicketGuids.Contains(ticket.TicketGuid);
		}
	}
	
	public void Release(Ticket ticket) {
		lock (this) {
			if (!activeTicketGuids.Remove(ticket.TicketGuid)) {
				return;
			}
			
			usedMemory -= ticket.MemoryAllocation;
			usedPorts.Remove(ticket.ServerPort);
			usedPorts.Remove(ticket.RconPort);
			
			RefreshAgentStatus();
			Logger.Debug("Released ticket {TicketGuid} (server port {ServerPort}, rcon port {RconPort}, memory allocation {MemoryAllocation} MB).", ticket.TicketGuid, ticket.ServerPort, ticket.RconPort, ticket.MemoryAllocation.InMegabytes);
		}
	}
	
	public void RefreshAgentStatus() {
		lock (this) {
			reportStatusQueue.Enqueue(new ReportAgentStatusMessage(activeTicketGuids.Count, usedMemory));
		}
	}
	
	public async Task Shutdown() {
		await reportStatusQueue.Shutdown(TimeSpan.FromSeconds(5));
	}
	
	public sealed record Ticket(Guid TicketGuid, RamAllocationUnits MemoryAllocation, ushort ServerPort, ushort RconPort);
}
