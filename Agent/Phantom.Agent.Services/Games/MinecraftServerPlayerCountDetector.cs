using System.Net.Sockets;
using Phantom.Agent.Services.Instances;
using Phantom.Common.Data.Agent.Instance.Backups;
using Phantom.Common.Data.Instance;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Games;

sealed class MinecraftServerPlayerCountDetector(InstanceContext instanceContext, ushort serverPort) : IInstancePlayerCountDetector {
	private readonly ILogger logger = PhantomLogger.Create<MinecraftServerPlayerCountDetector>(instanceContext.ShortName);
	
	private bool waitingForFirstDetection = true;
	
	public async Task<InstancePlayerCounts?> TryGetPlayerCounts(CancellationToken cancellationToken) {
		try {
			var playerCounts = await MinecraftServerStatusProtocol.GetPlayerCounts(serverPort, cancellationToken);
			waitingForFirstDetection = false;
			return playerCounts;
		} catch (MinecraftServerStatusProtocol.ProtocolException e) {
			logger.Error("{Message}", e.Message);
			return null;
		} catch (SocketException e) {
			bool waitingForServerStart = e.SocketErrorCode == SocketError.ConnectionRefused && waitingForFirstDetection;
			if (!waitingForServerStart) {
				logger.Warning("Could not check online player count. Socket error {ErrorCode} ({ErrorCodeName}), reason: {ErrorMessage}", e.ErrorCode, e.SocketErrorCode, e.Message);
			}
			
			return null;
		}
	}
}
