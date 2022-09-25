namespace Phantom.Server.Services.Instances; 

public enum InstanceState {
	Offline,
	Active,
	InstanceLimitExceeded,
	MemoryLimitExceeded
}
