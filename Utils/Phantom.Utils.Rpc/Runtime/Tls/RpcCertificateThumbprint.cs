using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Phantom.Utils.Rpc.Runtime.Tls;

public sealed record RpcCertificateThumbprint {
	private const int Length = 20;
	
	public static RpcCertificateThumbprint From(ReadOnlySpan<byte> bytes) {
		return new RpcCertificateThumbprint([..bytes]);
	}
	
	internal static RpcCertificateThumbprint From(X509Certificate certificate) {
		return new RpcCertificateThumbprint([..certificate.GetCertHash()]);
	}
	
	public ImmutableArray<byte> Bytes { get; }
	
	private RpcCertificateThumbprint(ImmutableArray<byte> bytes) {
		if (bytes.Length != Length) {
			throw new ArgumentOutOfRangeException(nameof(bytes), "Invalid thumbprint length: " + bytes.Length + ". Thumbprint length must be exactly " + Length + " bytes.");
		}
		
		this.Bytes = bytes;
	}
	
	internal bool Check(X509Certificate certificate) {
		return CryptographicOperations.FixedTimeEquals(Bytes.AsSpan(), certificate.GetCertHash());
	}
}
