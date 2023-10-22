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

	protected void Update(object? sender, EventArgs e) {
		Subs.Publish(GetData());
	}

	protected abstract T GetData();

	private sealed class Subscribers : EventSubscribers<T> {
		private readonly ObservableState<T> observer;
		
		public Subscribers(ILogger logger, ObservableState<T> observer) : base(logger) {
			this.observer = observer;
		}

		public override void Subscribe(object owner, Action<T> subscriber) {
			base.Subscribe(owner, subscriber);
			subscriber(observer.GetData());
		}
	}
}
