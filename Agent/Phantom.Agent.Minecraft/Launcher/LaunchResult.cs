using Phantom.Agent.Minecraft.Instance;

namespace Phantom.Agent.Minecraft.Launcher;

public abstract record LaunchResult {
	private LaunchResult() {}

	public sealed record Success(InstanceSession Session) : LaunchResult;

	public sealed record InvalidJavaRuntime : LaunchResult;
	
	public sealed record InvalidJvmArguments : LaunchResult;

	public sealed record CouldNotDownloadMinecraftServer : LaunchResult;
}
