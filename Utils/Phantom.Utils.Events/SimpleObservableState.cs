using Serilog;

namespace Phantom.Utils.Events;

public sealed class SimpleObservableState<T> : ObservableState<T> {
	public T Value { get; private set; }
	
	public SimpleObservableState(ILogger logger, T initialValue) : base(logger) {
		this.Value = initialValue;
	}
	
	public void SetTo(T newValue) {
		this.Value = newValue;
		Update();
	}
	
	protected override T GetData() {
		return Value;
	}
}
