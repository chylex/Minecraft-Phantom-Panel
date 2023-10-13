using System.Collections.Immutable;
using Phantom.Common.Data.Java;
using Phantom.Utils.Collections;

namespace Phantom.Controller.Services.Agents;

sealed class AgentJavaRuntimesManager {
	private readonly RwLockedDictionary<Guid, ImmutableArray<TaggedJavaRuntime>> runtimes = new (LockRecursionPolicy.NoRecursion);

	public ImmutableDictionary<Guid, ImmutableArray<TaggedJavaRuntime>> All => runtimes.ToImmutable();
	
	internal void Update(Guid agentGuid, ImmutableArray<TaggedJavaRuntime> runtimes) {
		this.runtimes[agentGuid] = runtimes;
	}
}
