using System.Collections.Immutable;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Tls;

namespace Phantom.Common.Data;

public readonly record struct ConnectionKey(RpcCertificateThumbprint CertificateThumbprint, AuthToken AuthToken) {
	private const byte TokenLength = AuthToken.Length;
	
	public ImmutableArray<byte> ToBytes() {
		Span<byte> result = stackalloc byte[TokenLength + CertificateThumbprint.Bytes.Length];
		AuthToken.ToBytes(result[..TokenLength]);
		CertificateThumbprint.Bytes.CopyTo(result[TokenLength..]);
		return [..result];
	}
	
	public static ConnectionKey FromBytes(ReadOnlySpan<byte> data) {
		var authToken = AuthToken.FromBytes(data[..TokenLength]);
		var certificateThumbprint = RpcCertificateThumbprint.From(data[TokenLength..]);
		return new ConnectionKey(certificateThumbprint, authToken);
	}
}
