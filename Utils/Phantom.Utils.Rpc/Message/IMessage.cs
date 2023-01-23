namespace Phantom.Utils.Rpc.Message; 

public interface IMessage<TListener, TReply> {
	Task<TReply> Accept(TListener listener);
}
