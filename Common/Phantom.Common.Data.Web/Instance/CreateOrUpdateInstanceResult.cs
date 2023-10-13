namespace Phantom.Common.Data.Web.Instance;

public enum CreateOrUpdateInstanceResult : byte {
	UnknownError,
	Success,
	InstanceNameMustNotBeEmpty,
	InstanceMemoryMustNotBeZero,
	MinecraftVersionDownloadInfoNotFound,
	AgentNotFound
}

public static class CreateOrUpdateInstanceResultExtensions {
	public static string ToSentence(this CreateOrUpdateInstanceResult reason) {
		return reason switch {
			CreateOrUpdateInstanceResult.Success                              => "Success.",
			CreateOrUpdateInstanceResult.InstanceNameMustNotBeEmpty           => "Instance name must not be empty.",
			CreateOrUpdateInstanceResult.InstanceMemoryMustNotBeZero          => "Memory must not be 0 MB.",
			CreateOrUpdateInstanceResult.MinecraftVersionDownloadInfoNotFound => "Could not find download information for the selected Minecraft version.",
			CreateOrUpdateInstanceResult.AgentNotFound                        => "Agent not found.",
			_                                                                 => "Unknown error."
		};
	}
}
