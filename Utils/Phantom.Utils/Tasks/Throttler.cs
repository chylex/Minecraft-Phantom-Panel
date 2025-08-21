namespace Phantom.Utils.Tasks;

public sealed class Throttler {
	private readonly TimeSpan interval;
	private DateTime lastInvocation;
	
	public Throttler(TimeSpan interval) {
		this.interval = interval;
		this.lastInvocation = DateTime.Now;
	}
	
	public bool Check() {
		var now = DateTime.Now;
		if (now - lastInvocation >= interval) {
			lastInvocation = now;
			return true;
		}
		
		return false;
	}
	
	public async Task Wait() {
		var now = DateTime.Now;
		var waitTime = lastInvocation + interval - now;
		if (waitTime > TimeSpan.Zero) {
			await Task.Delay(waitTime);
		}
		
		lastInvocation = DateTime.Now;
	}
}
