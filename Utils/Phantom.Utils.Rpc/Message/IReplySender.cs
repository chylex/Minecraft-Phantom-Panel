namespace Phantom.Utils.Rpc.Message;

interface IReplySender {
	Task SendReply(uint sequenceId, byte[] serializedReply);
}
