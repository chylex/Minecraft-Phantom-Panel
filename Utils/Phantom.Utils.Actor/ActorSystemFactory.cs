using Akka.Actor;
using Akka.Configuration;

namespace Phantom.Utils.Actor;

public static class ActorSystemFactory {
	private const string Configuration =
		"""
		akka {
		    actor {
		        default-dispatcher = {
		            executor = task-executor
		        }
		        internal-dispatcher = akka.actor.default-dispatcher
		        debug.unhandled = on
		    }
		    loggers = [
		        "Phantom.Utils.Actor.Logging.SerilogLogger, Phantom.Utils.Actor"
		    ]
		}
		unbounded-jump-ahead-mailbox {
		    mailbox-type : "Phantom.Utils.Actor.Mailbox.UnboundedJumpAheadMailbox, Phantom.Utils.Actor"
		}
		""";

	private static readonly BootstrapSetup Setup = BootstrapSetup.Create().WithConfig(ConfigurationFactory.ParseString(Configuration));

	public static ActorSystem Create(string name) {
		return ActorSystem.Create(name, Setup);
	}
}
