namespace Phantom.Utils.Rpc.Message;

enum MessageError : byte {
	InvalidData = 0,
	UnknownMessageRegistryCode = 1,
	MessageTooLarge = 2,
	MessageDeserializationError = 3,
	MessageHandlingError = 4,
	MessageReplyingError = 5,
	ReplyTooLarge = 6,
}

sealed class MessageErrorException : Exception {
	internal static MessageErrorException From(MessageError error) {
		return error switch {
			MessageError.InvalidData                 => new MessageErrorException("Invalid data.", error),
			MessageError.UnknownMessageRegistryCode  => new MessageErrorException("Unknown message registry code.", error),
			MessageError.MessageTooLarge             => new MessageErrorException("Message is too large.", error),
			MessageError.MessageDeserializationError => new MessageErrorException("Message deserialization error.", error),
			MessageError.MessageHandlingError        => new MessageErrorException("Message handling error.", error),
			MessageError.MessageReplyingError        => new MessageErrorException("Message replying error.", error),
			MessageError.ReplyTooLarge               => new MessageErrorException("Reply is too large.", error),
			_                                        => new MessageErrorException("Unknown error.", error),
		};
	}
	
	public MessageError Error { get; }
	
	internal MessageErrorException(string message, MessageError error) : base(message) {
		this.Error = error;
	}
}
