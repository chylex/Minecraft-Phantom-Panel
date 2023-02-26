using System.Collections.Immutable;

namespace Phantom.Agent.Minecraft.Launcher;

sealed record ServerJarInfo(string FilePath, ImmutableArray<string> ExtraArgs) {
	public ServerJarInfo(string filePath) : this(filePath, ImmutableArray<string>.Empty) {}
}
