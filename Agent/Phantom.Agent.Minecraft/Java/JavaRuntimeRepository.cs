using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Phantom.Common.Data.Java;
using Phantom.Utils.Cryptography;

namespace Phantom.Agent.Minecraft.Java;

public sealed class JavaRuntimeRepository {
	private readonly ImmutableDictionary<Guid, JavaRuntimeExecutable> runtimesByGuid;
	
	internal JavaRuntimeRepository(ImmutableArray<JavaRuntimeExecutable> runtimes) {
		var runtimesByGuidBuilder = ImmutableDictionary.CreateBuilder<Guid, JavaRuntimeExecutable>();
		
		foreach (JavaRuntimeExecutable runtime in runtimes) {
			runtimesByGuidBuilder.Add(GenerateStableGuid(runtime.ExecutablePath), runtime);
		}
		
		runtimesByGuid = runtimesByGuidBuilder.ToImmutable();
	}
	
	public ImmutableArray<TaggedJavaRuntime> All {
		get {
			return runtimesByGuid.Select(static kvp => new TaggedJavaRuntime(kvp.Key, kvp.Value.Runtime))
			                     .OrderBy(static taggedRuntime => taggedRuntime.Runtime)
			                     .ToImmutableArray();
		}
	}
	
	internal bool TryGetByGuid(Guid guid, [MaybeNullWhen(false)] out JavaRuntimeExecutable runtime) {
		return runtimesByGuid.TryGetValue(guid, out runtime);
	}
	
	private static Guid GenerateStableGuid(string executablePath) {
		Random rand = new Random(StableHashCode.ForString(executablePath));
		Span<byte> bytes = stackalloc byte[16];
		rand.NextBytes(bytes);
		return new Guid(bytes);
	}
}
