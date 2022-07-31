namespace Phantom.Server.Services;

public static class Services {
	public static AgentManager AgentManager { get; } = new (ServiceConfiguration.AuthToken ?? throw new Exception());
}
