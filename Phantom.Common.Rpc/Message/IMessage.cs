namespace Phantom.Common.Rpc.Message; 

public interface IMessage<TListener> {
	void Accept(TListener listener);
}
