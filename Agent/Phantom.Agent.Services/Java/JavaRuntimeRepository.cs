using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Phantom.Agent.Minecraft.Java;
using Phantom.Common.Data.Java;

namespace Phantom.Agent.Services.Java;

sealed class JavaRuntimeRepository {
	private readonly Dictionary<Guid, JavaRuntimeExecutable> runtimesByGuid = new ();
	private readonly Dictionary<string, Guid> guidsByPath = new ();
	private readonly ReaderWriterLockSlim rwLock = new (LockRecursionPolicy.NoRecursion);

	public ImmutableArray<TaggedJavaRuntime> All {
		get {
			rwLock.EnterReadLock();
			try {
				return runtimesByGuid.Select(static kvp => new TaggedJavaRuntime(kvp.Key, kvp.Value.Runtime)).ToImmutableArray();
			} finally {
				rwLock.ExitReadLock();
			}
		}
	}

	public void Include(JavaRuntimeExecutable runtime) {
		rwLock.EnterWriteLock();
		try {
			if (!guidsByPath.TryGetValue(runtime.ExecutablePath, out var guid)) {
				guidsByPath[runtime.ExecutablePath] = guid = Guid.NewGuid();
			}

			runtimesByGuid[guid] = runtime;
		} finally {
			rwLock.ExitWriteLock();
		}
	}

	public bool TryGetByGuid(Guid guid, [NotNullWhen(true)] out JavaRuntimeExecutable? runtime) {
		rwLock.EnterReadLock();
		try {
			return runtimesByGuid.TryGetValue(guid, out runtime);
		} finally {
			rwLock.ExitReadLock();
		}
	}
}
