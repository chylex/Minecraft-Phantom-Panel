using Phantom.Common.Data;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Tls;

namespace Phantom.Controller.Services.Agents;

sealed class AgentConnectionKeys(RpcCertificateThumbprint certificateThumbprint) {
	public ConnectionKey Get(AuthToken authToken) {
		return new ConnectionKey(certificateThumbprint, authToken);
	}
}
