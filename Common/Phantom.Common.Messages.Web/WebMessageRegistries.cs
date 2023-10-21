using Phantom.Common.Logging;
using Phantom.Common.Messages.Web.BiDirectional;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Web;

public static class WebMessageRegistries {
	public static MessageRegistry<IMessageToControllerListener> ToController { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToController)));
	public static MessageRegistry<IMessageToWebListener> ToWeb { get; } = new (PhantomLogger.Create("MessageRegistry", nameof(ToWeb)));
	
	public static IMessageDefinitions<IMessageToWebListener, IMessageToControllerListener, ReplyMessage> Definitions { get; } = new MessageDefinitions();

	static WebMessageRegistries() {
		ToController.Add<ReplyMessage>(127);
		
		ToWeb.Add<ReplyMessage>(127);
	}

	private sealed class MessageDefinitions : IMessageDefinitions<IMessageToWebListener, IMessageToControllerListener, ReplyMessage> {
		public MessageRegistry<IMessageToWebListener> ToClient => ToWeb;
		public MessageRegistry<IMessageToControllerListener> ToServer => ToController;

		public bool IsRegistrationMessage(Type messageType) {
			return false;
		}

		public ReplyMessage CreateReplyMessage(uint sequenceId, byte[] serializedReply) {
			return new ReplyMessage(sequenceId, serializedReply);
		}
	}
}
