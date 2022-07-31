namespace Phantom.Common.Rpc.Message; 

public interface IMessage<TListener> {
	Task Accept(TListener listener);
}
