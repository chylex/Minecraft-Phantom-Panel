using System.Diagnostics.CodeAnalysis;

namespace Phantom.Agent.Minecraft.Java; 

public interface IJavaRuntimeRepository {
	bool TryGetByGuid(Guid guid, [MaybeNullWhen(false)] out JavaRuntimeExecutable runtimeExecutable);
}
