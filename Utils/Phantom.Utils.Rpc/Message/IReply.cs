namespace Phantom.Utils.Rpc.Message; 

public interface IReply {
	uint SequenceId { get; }
	byte[] SerializedReply { get; }
}
