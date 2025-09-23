using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Tls;

namespace Phantom.Common.Data;

public readonly record struct ConnectionKey(RpcCertificateThumbprint CertificateThumbprint, AuthToken AuthToken) {
	private const byte TokenLength = AuthToken.Length;

	public byte[] ToBytes() {
		Span<byte> result = stackalloc byte[TokenLength + CertificateThumbprint.Bytes.Length];
		AuthToken.Bytes.CopyTo(result[..TokenLength]);
		CertificateThumbprint.Bytes.CopyTo(result[TokenLength..]);
		return result.ToArray();
	}

	public static ConnectionKey FromBytes(ReadOnlySpan<byte> data) {
		var authToken = new AuthToken([..data[..TokenLength]]);
		var certificateThumbprint = RpcCertificateThumbprint.From(data[TokenLength..]);
		return new ConnectionKey(certificateThumbprint, authToken);
	}
}
