using System.Collections.Immutable;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Server.Services.Instances; 

public sealed class InstanceManager {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceManager>();
	
	private readonly ObservableInstances instances = new ();
	
	public EventSubscribers<ImmutableArray<InstanceInfo>> InstancesChanged => instances.Subs;

	internal InstanceManager() {}
	
	private sealed class ObservableInstances : ObservableState<ImmutableArray<InstanceInfo>> {
		private readonly RwLockedDictionary<Guid, InstanceInfo> instances = new (LockRecursionPolicy.NoRecursion);
		
		protected override ImmutableArray<InstanceInfo> GetData() {
			return instances.ValuesCopy;
		}
	}
}
