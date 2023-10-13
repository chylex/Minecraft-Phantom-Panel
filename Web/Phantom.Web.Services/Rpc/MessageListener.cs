using Phantom.Common.Messages.Web;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Common.Messages.Web.ToWeb;
using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Tasks;
using Phantom.Web.Services.Agents;
using Phantom.Web.Services.Instances;

namespace Phantom.Web.Services.Rpc; 

public sealed class MessageListener : IMessageToWebListener {
	public TaskCompletionSource<bool> RegisterSuccessWaiter { get; } = AsyncTasks.CreateCompletionSource<bool>();

	private readonly RpcConnectionToServer<IMessageToControllerListener> connection;
	private readonly AgentManager agentManager;
	private readonly InstanceManager instanceManager;
	private readonly InstanceLogManager instanceLogManager;
	
	public MessageListener(RpcConnectionToServer<IMessageToControllerListener> connection, AgentManager agentManager, InstanceManager instanceManager, InstanceLogManager instanceLogManager) {
		this.connection = connection;
		this.agentManager = agentManager;
		this.instanceManager = instanceManager;
		this.instanceLogManager = instanceLogManager;
	}

	public Task<NoReply> HandleRegisterWebResult(RegisterWebResultMessage message) {
		RegisterSuccessWaiter.TrySetResult(message.Success);
		return Task.FromResult(NoReply.Instance);
	}

	public Task<NoReply> HandleRefreshAgents(RefreshAgentsMessage message) {
		agentManager.RefreshAgents(message.Agents);
		return Task.FromResult(NoReply.Instance);
	}

	public Task<NoReply> HandleRefreshInstances(RefreshInstancesMessage message) {
		instanceManager.RefreshInstances(message.Instances);
		return Task.FromResult(NoReply.Instance);
	}

	public Task<NoReply> HandleInstanceOutput(InstanceOutputMessage message) {
		instanceLogManager.AddLines(message.InstanceGuid, message.Lines);
		return Task.FromResult(NoReply.Instance);
	}

	public Task<NoReply> HandleReply(ReplyMessage message) {
		connection.Receive(message);
		return Task.FromResult(NoReply.Instance);
	}
}
