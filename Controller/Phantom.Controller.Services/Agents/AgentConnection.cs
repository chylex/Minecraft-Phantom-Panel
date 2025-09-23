using Phantom.Common.Messages.Agent;
using Phantom.Utils.Actor;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Runtime.Server;
using Serilog;

namespace Phantom.Controller.Services.Agents;

sealed class AgentConnection(Guid agentGuid, string agentName) {
	private static readonly ILogger Logger = PhantomLogger.Create<AgentConnection>();
	
	private string agentName = agentName;
	private RpcServerToClientConnection<IMessageToController, IMessageToAgent>? connection;
	
	public void SetAgentName(string newAgentName) {
		Volatile.Write(ref agentName, newAgentName);
	}
	
	public void UpdateConnection(RpcServerToClientConnection<IMessageToController, IMessageToAgent> newConnection) {
		lock (this) {
			if (connection != null) {
				_ = connection.CloseSession();
			}
			
			connection = newConnection;
		}
	}
	
	public void Close() {
		lock (this) {
			connection = null;
		}
	}
	
	public ValueTask Send<TMessage>(TMessage message) where TMessage : IMessageToAgent {
		lock (this) {
			if (connection != null) {
				return connection.MessageSender.Send(message);
			}
		}
		
		LogAgentOffline();
		return ValueTask.CompletedTask;
	}
	
	public Task<TReply?> Send<TMessage, TReply>(TMessage message, TimeSpan waitForReplyTime, CancellationToken waitForReplyCancellationToken) where TMessage : IMessageToAgent, ICanReply<TReply> where TReply : class {
		lock (this) {
			if (connection != null) {
				return connection.MessageSender.Send<TMessage, TReply>(message, waitForReplyTime, waitForReplyCancellationToken)!;
			}
		}
		
		LogAgentOffline();
		return Task.FromResult<TReply?>(null);
	}
	
	private void LogAgentOffline() {
		Logger.Error("Could not send message to offline agent \"{Name}\" (GUID {Guid}).", Volatile.Read(ref agentName), agentGuid);
	}
}
