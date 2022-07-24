using Phantom.Agent.Command;
using Phantom.Utils.Terminal;

namespace Phantom.Agent;

sealed class TestCommandListener : CommandListener {
	public override void OnCreateInstance(Guid instanceGuid) {
		Terminal.PrintLine("Created instance: " + instanceGuid);
	}

	public override void OnStartInstance(InstanceManager.LaunchResult result) {
		if (result is InstanceManager.LaunchResult.Success success) {
			var outputPrefix = "[" + success.InstanceGuid + "] ";
			success.Session.AddOutputListener((_, outputLine) => Terminal.PrintLine(outputPrefix + outputLine), uint.MaxValue);
		}
	}
}
