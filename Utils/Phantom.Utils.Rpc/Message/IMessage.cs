namespace Phantom.Utils.Rpc.Message; 

public interface IMessage<TListener, TReply> {
	public uint SequenceId { get; }
	Task<TReply> Accept(TListener listener);
}
