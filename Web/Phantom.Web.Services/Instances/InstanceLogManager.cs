using System.Collections.Concurrent;
using System.Collections.Immutable;
using Phantom.Common.Logging;
using Phantom.Utils.Collections;
using Phantom.Utils.Events;
using ILogger = Serilog.ILogger;

namespace Phantom.Web.Services.Instances; 

public sealed class InstanceLogManager {
	private const int RetainedLines = 1000;
	
	private readonly ConcurrentDictionary<Guid, ObservableInstanceLogs> logsByInstanceGuid = new ();
	
	private ObservableInstanceLogs GetInstanceLogs(Guid instanceGuid) {
		return logsByInstanceGuid.GetOrAdd(instanceGuid, static _ => new ObservableInstanceLogs(PhantomLogger.Create<InstanceLogManager, ObservableInstanceLogs>()));
	}

	internal void AddLines(Guid instanceGuid, ImmutableArray<string> lines) {
		GetInstanceLogs(instanceGuid).Add(lines);
	}
	
	public EventSubscribers<RingBuffer<string>> GetSubs(Guid instanceGuid) {
		return GetInstanceLogs(instanceGuid).Subs;
	}
	
	private sealed class ObservableInstanceLogs : ObservableState<RingBuffer<string>> {
		private readonly RingBuffer<string> log = new (RetainedLines);

		public ObservableInstanceLogs(ILogger logger) : base(logger) {}

		public void Add(ImmutableArray<string> lines) {
			foreach (var line in lines) {
				log.Add(InstanceLogHtmlFilters.Process(line));
			}

			Update();
		}

		protected override RingBuffer<string> GetData() {
			return log;
		}
	}
}
