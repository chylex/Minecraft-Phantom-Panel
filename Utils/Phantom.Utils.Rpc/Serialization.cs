namespace Phantom.Utils.Rpc;

static class Serialization {
	public const int GuidBytes = 16;
	
	public static void WriteGuid(Span<byte> buffer, Guid guid) {
		if (!guid.TryWriteBytes(buffer)) {
			throw new InvalidOperationException("Span is not large enough to write a GUID.");
		}
	}
}
