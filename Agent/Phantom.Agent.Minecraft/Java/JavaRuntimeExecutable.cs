using Phantom.Common.Data.Java;

namespace Phantom.Agent.Minecraft.Java;

public sealed record JavaRuntimeExecutable(string ExecutablePath, JavaRuntime Runtime);
