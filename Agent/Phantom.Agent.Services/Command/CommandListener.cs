namespace Phantom.Agent.Services.Command; 

abstract class CommandListener {
	public virtual void OnCreateInstance(Guid instanceGuid) {}
	public virtual void OnStartInstance(InstanceSessionManager.LaunchResult result) {}
	public virtual void OnSendCommandToInstance(InstanceSessionManager.SendCommandResult result) {}
}
