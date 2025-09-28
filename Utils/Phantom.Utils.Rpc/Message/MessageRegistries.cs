namespace Phantom.Utils.Rpc.Message;

public readonly record struct MessageRegistries<TClientToServerMessage, TServerToClientMessage>(
	MessageRegistry<TServerToClientMessage> ToClient,
	MessageRegistry<TClientToServerMessage> ToServer
) {
	internal WithMapping CreateMapping() {
		return new WithMapping(ToClient.CreateMapping(), ToServer.CreateMapping());
	}
	
	internal readonly record struct WithMapping(
		MessageRegistry<TServerToClientMessage>.WithMapping ToClient,
		MessageRegistry<TClientToServerMessage>.WithMapping ToServer
	);
}
