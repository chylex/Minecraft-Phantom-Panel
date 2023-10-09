namespace Phantom.Controller.Services.Instances;

public enum AddOrEditInstanceResult : byte {
	UnknownError,
	Success,
	InstanceNameMustNotBeEmpty,
	InstanceMemoryMustNotBeZero,
	MinecraftVersionDownloadInfoNotFound,
	AgentNotFound
}

public static class AddOrEditInstanceResultExtensions {
	public static string ToSentence(this AddOrEditInstanceResult reason) {
		return reason switch {
			AddOrEditInstanceResult.Success                              => "Success.",
			AddOrEditInstanceResult.InstanceNameMustNotBeEmpty           => "Instance name must not be empty.",
			AddOrEditInstanceResult.InstanceMemoryMustNotBeZero          => "Memory must not be 0 MB.",
			AddOrEditInstanceResult.MinecraftVersionDownloadInfoNotFound => "Could not find download information for the selected Minecraft version.",
			AddOrEditInstanceResult.AgentNotFound                        => "Agent not found.",
			_                                                            => "Unknown error."
		};
	}
}
