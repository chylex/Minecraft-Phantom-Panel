using Akka.Actor;
using Akka.Event;
using Phantom.Utils.Actor;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Utils.Rpc.Runtime;

sealed class RpcReceiverActor<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage> : ReceiveActor<RpcReceiverActor<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>.ReceiveMessageCommand>, IWithStash where TRegistrationMessage : TServerMessage where TReplyMessage : TClientMessage, TServerMessage {
	public readonly record struct Init(
		string LoggerName,
		IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> MessageDefinitions,
		IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> RegistrationHandler,
		RpcConnectionToClient<TClientMessage> Connection
	);

	public static Props<ReceiveMessageCommand> Factory(Init init) {
		return Props<ReceiveMessageCommand>.Create(() => new RpcReceiverActor<TClientMessage, TServerMessage, TRegistrationMessage, TReplyMessage>(init), new ActorConfiguration {
			SupervisorStrategy = SupervisorStrategies.Resume,
			StashCapacity = 100
		});
	}

	public IStash Stash { get; set; } = null!;

	private readonly string loggerName;
	private readonly IMessageDefinitions<TClientMessage, TServerMessage, TReplyMessage> messageDefinitions;
	private readonly IRegistrationHandler<TClientMessage, TServerMessage, TRegistrationMessage> registrationHandler;
	private readonly RpcConnectionToClient<TClientMessage> connection;

	private RpcReceiverActor(Init init) {
		this.loggerName = init.LoggerName;
		this.messageDefinitions = init.MessageDefinitions;
		this.registrationHandler = init.RegistrationHandler;
		this.connection = init.Connection;

		ReceiveAsync<ReceiveMessageCommand>(ReceiveMessageUnauthorized);
	}

	public sealed record ReceiveMessageCommand(Type MessageType, ReadOnlyMemory<byte> Data);

	private async Task ReceiveMessageUnauthorized(ReceiveMessageCommand command) {
		if (command.MessageType == typeof(TRegistrationMessage)) {
			await HandleRegistrationMessage(command);
		}
		else if (Stash.IsFull) {
			Context.GetLogger().Warning("Stash is full, dropping message: {MessageType}", command.MessageType);
		}
		else {
			Stash.Stash();
		}
	}

	private async Task HandleRegistrationMessage(ReceiveMessageCommand command) {
		if (!messageDefinitions.ToServer.Read(command.Data, out TRegistrationMessage message)) {
			return;
		}

		var props = await registrationHandler.TryRegister(connection, message);
		if (props == null) {
			return;
		}

		var handlerActor = Context.ActorOf(props, "Handler");
		var replySender = new ReplySender<TClientMessage, TReplyMessage>(connection, messageDefinitions);
		BecomeAuthorized(new MessageHandler<TServerMessage>(loggerName, handlerActor, replySender));
	}

	private void BecomeAuthorized(MessageHandler<TServerMessage> handler) {
		Stash.UnstashAll();

		Become(() => {
			Receive<ReceiveMessageCommand>(command => messageDefinitions.ToServer.Handle(command.Data, handler));
		});
	}
}
