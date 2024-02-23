namespace Phantom.Utils.Actor.Event;

public sealed class ObservableState<TState> {
	private readonly ReaderWriterLockSlim rwLock = new (LockRecursionPolicy.NoRecursion);
	private readonly List<IListener> listeners = new ();

	private TState state;

	public TState State {
		get {
			rwLock.EnterReadLock();
			try {
				return state;
			} finally {
				rwLock.ExitReadLock();
			}
		}
	}
	
	public Publisher PublisherSide { get; }
	public Receiver ReceiverSide { get; }
	
	public ObservableState(TState state) {
		this.state = state;
		this.PublisherSide = new Publisher(this);
		this.ReceiverSide = new Receiver(this);
	}

	private interface IListener {
		bool IsFor<TMessage>(ActorRef<TMessage> other);
		void Notify(TState state);
	}
	
	private readonly record struct Listener<TMessage>(ActorRef<TMessage> Actor, Func<TState, TMessage> MessageFactory) : IListener {
		public bool IsFor<TOtherMessage>(ActorRef<TOtherMessage> other) {
			return Actor.IsSame(other);
		}

		public void Notify(TState state) {
			Actor.Tell(MessageFactory(state));
		}
	}
	
	public readonly struct Publisher {
		private readonly ObservableState<TState> owner;
		
		internal Publisher(ObservableState<TState> owner) {
			this.owner = owner;
		}

		public void Publish(TState state) {
			Publish(static (_, newState) => newState, state);
		}
		
		public void Publish<TArg>(Func<TState, TArg, TState> stateUpdater, TArg userObject) {
			owner.rwLock.EnterWriteLock();
			try {
				SetInternalState(stateUpdater(owner.state, userObject));
			} finally {
				owner.rwLock.ExitWriteLock();
			}
		}
		
		public void Publish<TArg1, TArg2>(Func<TState, TArg1, TArg2, TState> stateUpdater, TArg1 userObject1, TArg2 userObject2) {
			owner.rwLock.EnterWriteLock();
			try {
				SetInternalState(stateUpdater(owner.state, userObject1, userObject2));
			} finally {
				owner.rwLock.ExitWriteLock();
			}
		}

		private void SetInternalState(TState state) {
			owner.state = state;

			foreach (var listener in owner.listeners) {
				listener.Notify(state);
			}
		}
	}

	public readonly struct Receiver {
		private readonly ObservableState<TState> owner;
		
		internal Receiver(ObservableState<TState> owner) {
			this.owner = owner;
		}

		public void Register<TMessage>(ActorRef<TMessage> actor, Func<TState, TMessage> messageFactory) {
			var listener = new Listener<TMessage>(actor, messageFactory);
			
			owner.rwLock.EnterReadLock();
			try {
				owner.listeners.Add(listener);
				listener.Notify(owner.state);
			} finally {
				owner.rwLock.ExitReadLock();
			}
		}

		public void Unregister<TMessage>(ActorRef<TMessage> actor) {
			owner.rwLock.EnterWriteLock();
			try {
				int index = owner.listeners.FindIndex(listener => listener.IsFor(actor));
				if (index != -1) {
					owner.listeners.RemoveAt(index);
				}
			} finally {
				owner.rwLock.ExitWriteLock();
			}
		}
	}
}
