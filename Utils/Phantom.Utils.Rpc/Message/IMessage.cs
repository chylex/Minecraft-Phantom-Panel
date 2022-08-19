namespace Phantom.Utils.Rpc.Message; 

public interface IMessage<TListener> {
	Task Accept(TListener listener);
}
