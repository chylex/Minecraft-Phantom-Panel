using System.Collections.Concurrent;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime.Server;

sealed class RpcServerClientSessions<TServerToClientMessage> {
	private readonly string loggerName;
	private readonly RpcServerConnectionParameters connectionParameters;
	private readonly MessageRegistry<TServerToClientMessage> messageRegistry;
	
	private readonly ConcurrentDictionary<Guid, RpcServerClientSession<TServerToClientMessage>> sessionsById = new ();
	
	private readonly Func<Guid, RpcServerClientSession<TServerToClientMessage>> createSessionFunction;
	private int nextSessionSequenceId;
	
	public int Count => sessionsById.Count;
	
	public RpcServerClientSessions(string loggerName, RpcServerConnectionParameters connectionParameters, MessageRegistry<TServerToClientMessage> messageRegistry) {
		this.loggerName = loggerName;
		this.connectionParameters = connectionParameters;
		this.messageRegistry = messageRegistry;
		this.createSessionFunction = CreateSession;
	}
	
	public RpcServerClientSession<TServerToClientMessage> GetOrCreateSession(Guid sessionId) {
		return sessionsById.GetOrAdd(sessionId, createSessionFunction);
	}
	
	private RpcServerClientSession<TServerToClientMessage> CreateSession(Guid sessionId) {
		return new RpcServerClientSession<TServerToClientMessage>(NextLoggerName(sessionId), connectionParameters, messageRegistry, this, sessionId);
	}
	
	private string NextLoggerName(Guid sessionId) {
		string name = PhantomLogger.ShortenGuid(sessionId);
		return PhantomLogger.ConcatNames(loggerName, name + "/" + Interlocked.Increment(ref nextSessionSequenceId));
	}
	
	public void Remove(RpcServerClientSession<TServerToClientMessage> session) {
		sessionsById.TryRemove(new KeyValuePair<Guid, RpcServerClientSession<TServerToClientMessage>>(session.SessionId, session));
	}
	
	public async Task CloseAll() {
		List<Task> tasks = [];
		
		foreach (Guid sessionId in sessionsById.Keys) {
			if (sessionsById.Remove(sessionId, out var session)) {
				tasks.Add(session.Close(closedByClient: false));
			}
		}
		
		await Task.WhenAll(tasks).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
	}
}
