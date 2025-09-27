using Serilog;

namespace Phantom.Utils.Events;

public sealed class SimpleObservableState<T>(ILogger logger, T initialValue) : ObservableState<T>(logger) {
	public T Value { get; private set; } = initialValue;
	
	public void SetTo(T newValue) {
		this.Value = newValue;
		Update();
	}
	
	protected override T GetData() {
		return Value;
	}
}
