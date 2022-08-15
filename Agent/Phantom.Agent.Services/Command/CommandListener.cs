namespace Phantom.Agent.Services.Command; 

public abstract class CommandListener {
	public virtual void OnCreateInstance(Guid instanceGuid) {}
	public virtual void OnStartInstance(InstanceManager.LaunchResult result) {}
	public virtual void OnSendCommandToInstance(InstanceManager.SendCommandResult result) {}
}
