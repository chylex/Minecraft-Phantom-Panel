namespace Phantom.Utils.Rpc.Message; 

public sealed class MessageQueueKey {
	public string Name { get; }

	public MessageQueueKey(string name) {
		Name = name;
	}
}
