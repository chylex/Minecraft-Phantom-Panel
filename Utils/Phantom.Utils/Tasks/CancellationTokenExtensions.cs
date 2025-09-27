namespace Phantom.Utils.Tasks;

public static class CancellationTokenExtensions {
	public static bool Check(this CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		return true;
	}
}
