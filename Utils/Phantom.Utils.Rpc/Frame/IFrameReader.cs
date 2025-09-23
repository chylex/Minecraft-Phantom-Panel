using Phantom.Utils.Rpc.Frame.Types;

namespace Phantom.Utils.Rpc.Frame;

interface IFrameReader {
	void OnSessionTerminationFrame();
	ValueTask OnPingFrame(DateTimeOffset pingTime, CancellationToken cancellationToken);
	void OnPongFrame(PongFrame frame);
	Task OnMessageFrame(MessageFrame frame, CancellationToken cancellationToken);
	void OnMessageReplyFrame(MessageReplyFrame frame);
	void OnMessageErrorFrame(MessageErrorFrame frame);
	void OnUnknownFrame(byte frameId);
}
