using Serilog;

namespace Phantom.Utils.Events;

public abstract class ObservableState<T> {
	public EventSubscribers<T> Subs { get; }
	
	protected ObservableState(ILogger logger) {
		Subs = new Subscribers(logger, this);
	}
	
	protected void Update() {
		Subs.Publish(GetData());
	}
	
	protected abstract T GetData();
	
	private sealed class Subscribers(ILogger logger, ObservableState<T> observer) : EventSubscribers<T>(logger) {
		public override void Subscribe(object owner, Action<T> subscriber) {
			base.Subscribe(owner, subscriber);
			subscriber(observer.GetData());
		}
	}
}
