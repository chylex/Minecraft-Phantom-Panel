using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Frame;
using Phantom.Utils.Rpc.Frame.Types;
using Phantom.Utils.Rpc.Message;
using Serilog;

namespace Phantom.Utils.Rpc.Runtime;

sealed class RpcFrameReader<TSentMessage, TReceivedMessage>(
	string loggerName,
	RpcCommonConnectionParameters connectionParameters,
	MessageTypeMapping<TReceivedMessage> messageTypeMapping,
	MessageHandler<TReceivedMessage> messageHandler,
	MessageSender<TSentMessage> messageSender,
	RpcFrameSender<TSentMessage> frameSender
) : IFrameReader {
	private readonly ILogger logger = PhantomLogger.Create<RpcFrameReader<TSentMessage, TReceivedMessage>>(loggerName);
	
	private readonly ushort maxConcurrentlyHandledMessages = connectionParameters.MaxConcurrentlyHandledMessages;
	private readonly SemaphoreSlim messageHandlingSemaphore = new (connectionParameters.MaxConcurrentlyHandledMessages);
	
	public void OnSessionTerminationFrame() {
		messageHandler.Receiver.OnSessionTerminated();
	}
	
	public ValueTask OnPingFrame(DateTimeOffset pingTime, CancellationToken cancellationToken) {
		messageHandler.OnPing();
		return frameSender.SendPong(pingTime, cancellationToken);
	}
	
	public void OnPongFrame(PongFrame frame) {
		frameSender.ReceivePong(frame);
	}
	
	public async Task OnMessageFrame(MessageFrame frame, CancellationToken cancellationToken) {
		if (!frameSender.ReceiveMessage(frame)) {
			logger.Warning("Received duplicate message {MessageId}.", frame.MessageId);
			return;
		}
		
		if (messageTypeMapping.TryGetType(frame, out var messageType)) {
			logger.Debug("Received message {MesageId} of type {MessageType} ({Bytes} B).", frame.MessageId, messageType.Name, frame.SerializedMessage.Length);
		}
		
		Task acquireSemaphore = messageHandlingSemaphore.WaitAsync(cancellationToken);
		try {
			if (!acquireSemaphore.IsCompleted) {
				logger.Warning("Reached limit for concurrently handled messages ({Limit}).", maxConcurrentlyHandledMessages);
			}
			
			await acquireSemaphore;
			_ = HandleMessage(frame, cancellationToken);
		} catch (Exception) {
			messageHandlingSemaphore.Release();
			throw;
		}
	}
	
	private async Task HandleMessage(MessageFrame frame, CancellationToken cancellationToken) {
		try {
			await messageTypeMapping.Handle(frame, messageHandler, cancellationToken);
		} finally {
			messageHandlingSemaphore.Release();
		}
	}
	
	public void OnMessageReplyFrame(MessageReplyFrame frame) {
		messageSender.ReceiveReply(frame);
	}
	
	public void OnMessageErrorFrame(MessageErrorFrame frame) {
		messageSender.ReceiveError(frame);
	}
	
	public void OnUnknownFrame(byte frameId) {
		logger.Error("Received unknown frame ID: {FrameId}", frameId);
	}
}
