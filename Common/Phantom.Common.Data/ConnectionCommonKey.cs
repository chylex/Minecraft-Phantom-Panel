namespace Phantom.Common.Data;

public readonly record struct ConnectionCommonKey(byte[] CertificatePublicKey, AuthToken AuthToken) {
	private const byte TokenLength = AuthToken.Length;
	
	public byte[] ToBytes() {
		Span<byte> result = stackalloc byte[TokenLength + CertificatePublicKey.Length];
		AuthToken.WriteTo(result[..TokenLength]);
		CertificatePublicKey.CopyTo(result[TokenLength..]);
		return result.ToArray();
	}
	
	public static ConnectionCommonKey FromBytes(byte[] agentKey) {
		var authToken = new AuthToken(agentKey[..TokenLength]);
		var certificatePublicKey = agentKey[TokenLength..];
		return new ConnectionCommonKey(certificatePublicKey, authToken);
	}
}
