using Phantom.Agent.Services.Command;

namespace Phantom.Agent.Services;

public sealed class TestCommandListener : CommandListener {
	public override void OnCreateInstance(Guid instanceGuid) {
		Console.WriteLine("Created instance: " + instanceGuid);
	}

	public override void OnStartInstance(InstanceManager.LaunchResult result) {
		if (result is InstanceManager.LaunchResult.Success success) {
			var outputPrefix = "[" + success.InstanceGuid + "] ";
			success.Session.AddOutputListener((_, outputLine) => Console.WriteLine(outputPrefix + outputLine), uint.MaxValue);
		}
	}
}
