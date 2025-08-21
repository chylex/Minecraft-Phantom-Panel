using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Phantom.Common.Data.Java;
using Phantom.Utils.Cryptography;

namespace Phantom.Agent.Minecraft.Java;

public sealed class JavaRuntimeRepository {
	private readonly Dictionary<string, Guid> guidsByPath = new ();
	private readonly Dictionary<Guid, JavaRuntimeExecutable> runtimesByGuid = new ();
	private readonly ReaderWriterLockSlim rwLock = new (LockRecursionPolicy.NoRecursion);
	
	public ImmutableArray<TaggedJavaRuntime> All {
		get {
			rwLock.EnterReadLock();
			try {
				return runtimesByGuid.Select(static kvp => new TaggedJavaRuntime(kvp.Key, kvp.Value.Runtime)).OrderBy(static taggedRuntime => taggedRuntime.Runtime).ToImmutableArray();
			} finally {
				rwLock.ExitReadLock();
			}
		}
	}
	
	public void Include(JavaRuntimeExecutable runtime) {
		rwLock.EnterWriteLock();
		try {
			if (!guidsByPath.TryGetValue(runtime.ExecutablePath, out var guid)) {
				guidsByPath[runtime.ExecutablePath] = guid = GenerateStableGuid(runtime.ExecutablePath);
			}
			
			runtimesByGuid[guid] = runtime;
		} finally {
			rwLock.ExitWriteLock();
		}
	}
	
	public bool TryGetByGuid(Guid guid, [MaybeNullWhen(false)] out JavaRuntimeExecutable runtime) {
		rwLock.EnterReadLock();
		try {
			return runtimesByGuid.TryGetValue(guid, out runtime);
		} finally {
			rwLock.ExitReadLock();
		}
	}
	
	private static Guid GenerateStableGuid(string executablePath) {
		Random rand = new Random(StableHashCode.ForString(executablePath));
		Span<byte> bytes = stackalloc byte[16];
		rand.NextBytes(bytes);
		return new Guid(bytes);
	}
}
