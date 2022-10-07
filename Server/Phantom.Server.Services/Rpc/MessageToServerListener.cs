using Phantom.Common.Data.Replies;
using Phantom.Common.Messages;
using Phantom.Common.Messages.ToAgent;
using Phantom.Common.Messages.ToServer;
using Phantom.Server.Rpc;
using Phantom.Server.Services.Agents;

namespace Phantom.Server.Services.Rpc;

public sealed class MessageToServerListener : IMessageToServerListener {
	private readonly RpcClientConnection connection;
	private readonly CancellationToken cancellationToken;
	private readonly AgentManager agentManager;
	private readonly AgentJavaRuntimesManager agentJavaRuntimesManager;

	private Guid? agentGuid;
	private readonly TaskCompletionSource<Guid> agentGuidWaiter = new ();

	public bool IsDisposed { get; private set; }

	internal MessageToServerListener(RpcClientConnection connection, ServiceConfiguration configuration, AgentManager agentManager, AgentJavaRuntimesManager agentJavaRuntimesManager) {
		this.connection = connection;
		this.cancellationToken = configuration.CancellationToken;
		this.agentManager = agentManager;
		this.agentJavaRuntimesManager = agentJavaRuntimesManager;
	}

	public async Task HandleRegisterAgent(RegisterAgentMessage message) {
		if (agentGuid != null && agentGuid != message.AgentInfo.Guid) {
			await connection.Send(new RegisterAgentFailureMessage(RegisterAgentFailure.ConnectionAlreadyHasAnAgent));
		}
		else if (await agentManager.RegisterAgent(message.AuthToken, message.AgentInfo, connection)) {
			var guid = message.AgentInfo.Guid;
			agentGuid = guid;
			agentGuidWaiter.SetResult(guid);
		}
	}

	private async Task<Guid> WaitForAgentGuid() {
		return await agentGuidWaiter.Task.WaitAsync(cancellationToken);
	}
	
	public Task HandleUnregisterAgent(UnregisterAgentMessage message) {
		IsDisposed = true;
		agentManager.UnregisterAgent(message.AgentGuid, connection);
		return Task.CompletedTask;
	}

	public async Task HandleAgentIsAlive(AgentIsAliveMessage message) {
		agentManager.NotifyAgentIsAlive(await WaitForAgentGuid());
	}

	public async Task HandleAdvertiseJavaRuntimes(AdvertiseJavaRuntimesMessage message) {
		agentJavaRuntimesManager.Update(await WaitForAgentGuid(), message.Runtimes);
	}
}
