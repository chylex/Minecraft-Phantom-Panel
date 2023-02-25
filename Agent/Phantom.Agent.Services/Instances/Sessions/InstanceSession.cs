using Phantom.Agent.Minecraft.Instance;

namespace Phantom.Agent.Services.Instances.Sessions; 

sealed class InstanceSession : IDisposable {
	private readonly InstanceProcess process;
	private readonly InstanceContext context;
	private readonly InstanceLogSender logSender;

	public InstanceSession(InstanceProcess process, InstanceContext context) {
		this.process = process;
		this.context = context;
		this.logSender = new InstanceLogSender(context.Services.TaskManager, context.Configuration.InstanceGuid, context.ShortName);
		
		this.process.AddOutputListener(SessionOutput);
	}
	
	private void SessionOutput(object? sender, string line) {
		context.Logger.Verbose("[Server] {Line}", line);
		logSender.Enqueue(line);
	}
	
	public void Dispose() {
		logSender.Stop();
		process.Dispose();
		context.Services.PortManager.Release(context.Configuration);
	}
}
