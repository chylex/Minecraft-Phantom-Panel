namespace Phantom.Utils.Rpc.Message; 

public readonly struct NoReply {
	public static NoReply Instance { get; } = new ();
}
