namespace Phantom.Utils.Events; 

public abstract class ObservableState<T> {
	public EventSubscribers<T> Subs { get; }

	protected ObservableState() {
		Subs = new Subscribers(this);
	}

	protected void Update() {
		Subs.Publish(GetData());
	}
	
	protected abstract T GetData();

	private sealed class Subscribers : EventSubscribers<T> {
		private readonly ObservableState<T> observer;
		
		public Subscribers(ObservableState<T> observer) {
			this.observer = observer;
		}

		public override void Subscribe(object owner, Action<T> subscriber) {
			base.Subscribe(owner, subscriber);
			subscriber(observer.GetData());
		}
	}
}
