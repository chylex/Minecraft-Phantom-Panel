using System.Collections.Immutable;
using Phantom.Common.Messages.Agent.Handshake;
using Phantom.Common.Messages.Agent.ToAgent;
using Phantom.Utils.Logging;
using Phantom.Utils.Rpc.Message;
using Phantom.Utils.Rpc.Runtime;
using Phantom.Utils.Rpc.Runtime.Client;
using Serilog;

namespace Phantom.Agent.Services.Rpc;

public sealed class ControllerHandshake(AgentRegistration registration, AgentRegistrationHandler registrationHandler) : IRpcClientHandshake {
	private const int MaxInstances = 100_000;
	private const int MaxMessageBytes = 1024 * 1024 * 8;
	
	private readonly ILogger logger = PhantomLogger.Create<ControllerHandshake>();
	
	public async Task Perform(RpcStream stream, CancellationToken cancellationToken) {
		logger.Information("Registering with the controller...");
		
		ReadOnlyMemory<byte> serializedRegistration = MessageSerialization.Serialize(registration);
		await stream.WriteSignedInt(serializedRegistration.Length, cancellationToken);
		await stream.WriteBytes(serializedRegistration, cancellationToken);
		await stream.Flush(cancellationToken);
		
		if (await stream.ReadByte(cancellationToken) == 0) {
			return;
		}
		
		uint configureInstanceMessageCount = await stream.ReadUnsignedInt(cancellationToken);
		if (configureInstanceMessageCount > MaxInstances) {
			throw new InvalidOperationException("Trying to configure too many instances (" + configureInstanceMessageCount + " > " + MaxInstances + ").");
		}
		
		var configureInstanceMessages = ImmutableArray.CreateBuilder<ConfigureInstanceMessage>();
		
		for (int index = 0; index < configureInstanceMessageCount; index++) {
			int serializedMessageLength = await stream.ReadSignedInt(cancellationToken);
			if (serializedMessageLength is < 0 or > MaxMessageBytes) {
				throw new InvalidOperationException("Message must be between 0 and " + MaxMessageBytes + " bytes.");
			}
			
			var serializedMessage = await stream.ReadBytes(serializedMessageLength, cancellationToken);
			configureInstanceMessages.Add(MessageSerialization.Deserialize<ConfigureInstanceMessage>(serializedMessage));
		}
		
		registrationHandler.OnRegistrationComplete(configureInstanceMessages.ToImmutable());
		logger.Information("Registration complete.");
	}
}
