using Phantom.Common.Messages.Agent;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentConnection {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentConnection>();
	
	private readonly Guid agentGuid;
	private string agentName;
	
	private RpcConnectionToClient<IMessageToAgent>? connection;
	
	public AgentConnection(Guid agentGuid, string agentName) {
		this.agentName = agentName;
		this.agentGuid = agentGuid;
	}
	
	public void UpdateConnection(RpcConnectionToClient<IMessageToAgent> newConnection, string newAgentName) {
		lock (this) {
			connection?.Close();
			connection = newConnection;
			agentName = newAgentName;
		}
	}
	
	public bool CloseIfSame(RpcConnectionToClient<IMessageToAgent> expected) {
		lock (this) {
			if (connection != null && connection.IsSame(expected)) {
				connection.Close();
				return true;
			}
		}
		
		return false;
	}
	
	public Task Send<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		lock (this) {
			if (connection == null) {
				LogAgentOffline();
				return Task.CompletedTask;
			}
			
			return connection.Send(message);
		}
	}
	
	public Task<TReply?> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToAgent, ICanReply<TReply> where TReply : class {
		lock (this) {
			if (connection == null) {
				LogAgentOffline();
				return Task.FromResult<TReply?>(default);
			}
			
			return connection.Send<TMessage, TReply>(message, waitForReplyTime, waitForReplyCancellationToken)!;
		}
	}
	
	private void LogAgentOffline() {
		Logger.Error("Could not send message to offline agent \"{Name}\" (GUID {Guid}).", agentName, agentGuid);
	}
}
