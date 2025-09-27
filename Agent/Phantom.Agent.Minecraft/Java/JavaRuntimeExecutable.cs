using Phantom.Common.Data.Java;

namespace Phantom.Agent.Minecraft.Java;

sealed record JavaRuntimeExecutable(string ExecutablePath, JavaRuntime Runtime);
