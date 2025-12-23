using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Akka.Util;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime.Server;

sealed class RpcServerClientSessions<TServerToClientMessage>(
	string loggerName,
	RpcServerConnectionParameters connectionParameters,
	MessageTypeMapping<TServerToClientMessage> messageTypeMapping
) {
	private readonly ConcurrentDictionary<Guid, SessionHolder> sessionsByClientGuid = new ();
	private readonly ConcurrentSet<Guid> closedSessions = [];
	
	public int Count => sessionsByClientGuid.Count(static kvp => kvp.Value.IsActive);
	
	private int nextSessionSequenceId;
	
	public async Task<RpcServerClientSession<TServerToClientMessage>?> GetOrCreateSession(Guid clientGuid, Guid sessionGuid) {
		if (closedSessions.Contains(sessionGuid)) {
			return null;
		}
		
		var sessionHolder = sessionsByClientGuid.GetOrAdd(clientGuid, static (clientGuid, sessions) => new SessionHolder(clientGuid, sessions), this);
		return await sessionHolder.GetOrReplaceSession(sessionGuid);
	}
	
	private RpcServerClientSession<TServerToClientMessage> CreateSession(Guid clientGuid, Guid sessionGuid) {
		return new RpcServerClientSession<TServerToClientMessage>(NextLoggerName(clientGuid), connectionParameters, messageTypeMapping, this, clientGuid, sessionGuid);
	}
	
	private string NextLoggerName(Guid sessionGuid) {
		string name = PhantomLogger.ShortenGuid(sessionGuid);
		return PhantomLogger.ConcatNames(loggerName, name + "/" + Interlocked.Increment(ref nextSessionSequenceId));
	}
	
	public void Remove(RpcServerClientSession<TServerToClientMessage> session) {
		if (sessionsByClientGuid.TryGetValue(session.ClientGuid, out var sessionHolder)) {
			closedSessions.TryAdd(session.SessionGuid);
			sessionHolder.ForgetSession(session.SessionGuid);
		}
	}
	
	public async Task CloseAll() {
		List<Task> tasks = [];
		
		foreach (Guid sessionGuid in sessionsByClientGuid.Keys) {
			if (sessionsByClientGuid.Remove(sessionGuid, out var sessionHolder)) {
				tasks.Add(sessionHolder.CloseSession());
			}
		}
		
		await Task.WhenAll(tasks).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
	}
	
	private sealed class SessionHolder(Guid clientGuid, RpcServerClientSessions<TServerToClientMessage> sessions) {
		private readonly Lock @lock = new ();
		private RpcServerClientSession<TServerToClientMessage>? session;
		
		[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
		public bool IsActive => Volatile.Read(ref session) != null;
		
		public async Task<RpcServerClientSession<TServerToClientMessage>> GetOrReplaceSession(Guid sessionGuid) {
			RpcServerClientSession<TServerToClientMessage>? createdSession;
			RpcServerClientSession<TServerToClientMessage>? replacedSession;
			
			lock (@lock) {
				if (session != null && session.SessionGuid == sessionGuid) {
					return session;
				}
				else {
					replacedSession = session;
				}
				
				createdSession = sessions.CreateSession(clientGuid, sessionGuid);
				session = createdSession;
			}
			
			if (replacedSession != null) {
				await CloseSession(replacedSession);
			}
			
			return createdSession;
		}
		
		public void ForgetSession(Guid sessionGuid) {
			lock (@lock) {
				if (session != null && session.SessionGuid == sessionGuid) {
					session = null;
				}
			}
		}
		
		public async Task CloseSession() {
			RpcServerClientSession<TServerToClientMessage>? sessionToClose;
			lock (@lock) {
				sessionToClose = session;
				session = null;
			}
			
			if (sessionToClose != null) {
				await CloseSession(sessionToClose);
			}
		}
		
		private static async Task CloseSession(RpcServerClientSession<TServerToClientMessage> session) {
			await session.Close(closedByClient: false).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		}
	}
}
