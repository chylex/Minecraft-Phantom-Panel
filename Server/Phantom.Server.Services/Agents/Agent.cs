using Phantom.Common.Data;

namespace Phantom.Server.Services.Agents; 

public abstract class Agent {
	public abstract Guid Guid { get; }
	public abstract string Name { get; }
	public abstract AgentInfo? Info { get; }

	private Agent() {}

	public abstract Agent AsOffline();

	public sealed class Offline : Agent {
		public override Guid Guid { get; }
		public override string Name { get; }
		
		public override AgentInfo? Info => null;

		internal Offline(Guid guid, string name) {
			Guid = guid;
			Name = name;
		}

		public override Agent AsOffline() {
			return this;
		}
	}

	public sealed class Online : Agent {
		public override Guid Guid => Info.Guid;
		public override string Name => Info.Name;
		
		public override AgentInfo Info { get; }
		internal AgentConnection Connection { get; }

		internal Online(AgentConnection connection) {
			Connection = connection;
			Info = connection.Info;
		}

		public override Agent AsOffline() {
			return new Offline(Guid, Name);
		}
	}
}
