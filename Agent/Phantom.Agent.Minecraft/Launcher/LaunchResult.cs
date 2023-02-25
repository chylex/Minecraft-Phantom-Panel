using Phantom.Agent.Minecraft.Instance;

namespace Phantom.Agent.Minecraft.Launcher;

public abstract record LaunchResult {
	private LaunchResult() {}

	public sealed record Success(InstanceProcess Process) : LaunchResult;

	public sealed record InvalidJavaRuntime : LaunchResult;
	
	public sealed record InvalidJvmArguments : LaunchResult;

	public sealed record CouldNotDownloadMinecraftServer : LaunchResult;
	
	public sealed record CouldNotConfigureMinecraftServer : LaunchResult;
	
	public sealed record CouldNotStartMinecraftServer : LaunchResult;
}
