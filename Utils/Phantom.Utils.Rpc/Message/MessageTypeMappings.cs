namespace Phantom.Utils.Rpc.Message;

readonly record struct MessageTypeMappings<TClientToServerMessage, TServerToClientMessage>(
	MessageTypeMapping<TServerToClientMessage> ToClient,
	MessageTypeMapping<TClientToServerMessage> ToServer
);
