namespace Phantom.Common.Data.Agent;

public static class AgentKeyData {
	private const byte TokenLength = AgentAuthToken.Length;

	public static byte[] ToBytes(byte[] publicKey, AgentAuthToken agentToken) {
		Span<byte> agentKey = stackalloc byte[TokenLength + publicKey.Length];
		agentToken.WriteTo(agentKey[..TokenLength]);
		publicKey.CopyTo(agentKey[TokenLength..]);
		return agentKey.ToArray();
	}

	public static (byte[] PublicKey, AgentAuthToken AgentToken) FromBytes(byte[] agentKey) {
		var token = new AgentAuthToken(agentKey[..TokenLength]);
		var publicKey = agentKey[TokenLength..];
		return (publicKey, token);
	}
}
