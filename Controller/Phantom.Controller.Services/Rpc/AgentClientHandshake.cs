using System.Collections.Immutable;
using Phantom.Common.Messages.Agent.Handshake;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Controller.Services.Agents;
using Phantom.Utils.Monads;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Rpc.Runtime.Server;

namespace Phantom.Controller.Services.Rpc;

sealed class AgentClientHandshake : IRpcServerClientHandshake {
	private const int MaxRegistrationBytes = 1024 * 1024 * 8;
	
	private readonly AgentManager agentManager;
	
	internal AgentClientHandshake(AgentManager agentManager) {
		this.agentManager = agentManager;
	}
	
	public async Task Perform(bool isNewSession, RpcStream stream, Guid agentGuid, CancellationToken cancellationToken) {
		RegistrationResult registrationResult;
		switch (await RegisterAgent(stream, agentGuid, cancellationToken)) {
			case Left<RegistrationResult, Exception>(var result):
				await stream.WriteByte(value: 1, cancellationToken);
				registrationResult = result;
				break;
			
			case Right<RegistrationResult, Exception>(var exception):
				await stream.WriteByte(value: 0, cancellationToken);
				throw exception;
			
			default:
				await stream.WriteByte(value: 0, cancellationToken);
				throw new InvalidOperationException("Invalid result type.");
		}
		
		if (isNewSession) {
			await stream.WriteUnsignedInt((uint) registrationResult.ConfigureInstanceMessages.Length, cancellationToken);
			
			foreach (var configureInstanceMessage in registrationResult.ConfigureInstanceMessages) {
				ReadOnlyMemory<byte> serializedMessage = MessageSerialization.Serialize(configureInstanceMessage);
				await stream.WriteSignedInt(serializedMessage.Length, cancellationToken);
				await stream.WriteBytes(serializedMessage, cancellationToken);
			}
		}
		else {
			await stream.WriteUnsignedInt(value: 0, cancellationToken);
		}
		
		await stream.Flush(cancellationToken);
	}
	
	private async Task<Either<RegistrationResult, Exception>> RegisterAgent(RpcStream stream, Guid agentGuid, CancellationToken cancellationToken) {
		int serializedRegistrationLength = await stream.ReadSignedInt(cancellationToken);
		if (serializedRegistrationLength is < 0 or > MaxRegistrationBytes) {
			return Either.Right<Exception>(new InvalidOperationException("Registration must be between 0 and " + MaxRegistrationBytes + " bytes."));
		}
		
		var serializedRegistration = await stream.ReadBytes(serializedRegistrationLength, cancellationToken);
		
		AgentRegistration registration;
		try {
			registration = MessageSerialization.Deserialize<AgentRegistration>(serializedRegistration);
		} catch (Exception e) {
			return Either.Right<Exception>(new InvalidOperationException("Caught exception during deserialization.", e));
		}
		
		var configureInstanceMessages = await agentManager.RegisterAgent(agentGuid, registration);
		if (configureInstanceMessages == null) {
			return Either.Right<Exception>(new InvalidOperationException("Could not register agent."));
		}
		
		return Either.Left(new RegistrationResult(configureInstanceMessages.Value));
	}
	
	private readonly record struct RegistrationResult(ImmutableArray<ConfigureInstanceMessage> ConfigureInstanceMessages);
}
