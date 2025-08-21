namespace Phantom.Utils.Cryptography;

public sealed class Sha1String {
	public static Sha1String FromString(string? hash) {
		if (hash is not { Length: 40 } || hash.Any(static c => !char.IsAsciiHexDigit(c))) {
			throw new ArgumentException("Invalid SHA-1 hash.", nameof(hash));
		}
		
		return new Sha1String(hash.ToLowerInvariant());
	}
	
	public static Sha1String FromBytes(byte[] bytes) {
		return FromString(Convert.ToHexString(bytes));
	}
	
	private readonly string hash;
	
	private Sha1String(string hash) {
		this.hash = hash;
	}
	
	public override bool Equals(object? obj) {
		return obj is Sha1String other && hash == other.hash;
	}
	
	public override int GetHashCode() {
		return hash.GetHashCode();
	}
	
	public override string ToString() {
		return hash;
	}
}
