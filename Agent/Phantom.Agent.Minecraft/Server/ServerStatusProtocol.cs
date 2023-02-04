using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Minecraft.Server; 

public sealed class ServerStatusProtocol {
	private readonly ILogger logger;

	public ServerStatusProtocol(string loggerName) {
		this.logger = PhantomLogger.Create<ServerStatusProtocol>(loggerName);
	}

	public async Task<int?> GetOnlinePlayerCount(int serverPort, CancellationToken cancellationToken) {
		try {
			return await GetOnlinePlayerCountOrThrow(serverPort, cancellationToken);
		} catch (Exception e) {
			logger.Error(e, "Caught exception while checking if players are online.");
			return null;
		}
	}

	private async Task<int?> GetOnlinePlayerCountOrThrow(int serverPort, CancellationToken cancellationToken) {
		using var tcpClient = new TcpClient();
		await tcpClient.ConnectAsync(IPAddress.Loopback, serverPort, cancellationToken);
		var tcpStream = tcpClient.GetStream();
		
		// https://wiki.vg/Server_List_Ping
		tcpStream.WriteByte(0xFE);
		await tcpStream.FlushAsync(cancellationToken);

		short? messageLength = await ReadStreamHeader(tcpStream, cancellationToken);
		return messageLength == null ? null : await ReadOnlinePlayerCount(tcpStream, messageLength.Value * 2, cancellationToken);
	}

	private async Task<short?> ReadStreamHeader(NetworkStream tcpStream, CancellationToken cancellationToken) {
		var headerBuffer = ArrayPool<byte>.Shared.Rent(3);
		try {
			await tcpStream.ReadExactlyAsync(headerBuffer, 0, 3, cancellationToken);

			if (headerBuffer[0] != 0xFF) {
				logger.Error("Unexpected first byte in response from server: {FirstByte}.", headerBuffer[0]);
				return null;
			}

			short messageLength = BinaryPrimitives.ReadInt16BigEndian(headerBuffer.AsSpan(1));
			if (messageLength <= 0) {
				logger.Error("Unexpected message length in response from server: {MessageLength}.", messageLength);
				return null;
			}
			
			return messageLength;
		} finally {
			ArrayPool<byte>.Shared.Return(headerBuffer);
		}
	}

	private async Task<int?> ReadOnlinePlayerCount(NetworkStream tcpStream, int messageLength, CancellationToken cancellationToken) {
		var messageBuffer = ArrayPool<byte>.Shared.Rent(messageLength);
		try {
			await tcpStream.ReadExactlyAsync(messageBuffer, 0, messageLength, cancellationToken);

			// Valid response separator encoded in UTF-16BE is 0x00 0xA7 (§).
			const byte SeparatorSecondByte = 0xA7;
			
			static bool IsValidSeparator(ReadOnlySpan<byte> buffer, int index) {
				return index > 0 && buffer[index - 1] == 0x00;
			}
			
			int separator2 = Array.LastIndexOf(messageBuffer, SeparatorSecondByte);
			int separator1 = separator2 == -1 ? -1 : Array.LastIndexOf(messageBuffer, SeparatorSecondByte, separator2 - 1);
			if (!IsValidSeparator(messageBuffer, separator1) || !IsValidSeparator(messageBuffer, separator2)) {
				logger.Error("Could not find message separators in response from server.");
				return null;
			}

			string onlinePlayerCountStr = Encoding.BigEndianUnicode.GetString(messageBuffer[(separator1 + 1)..(separator2 - 1)]);
			if (!int.TryParse(onlinePlayerCountStr, out int onlinePlayerCount)) {
				logger.Error("Could not parse online player count in response from server: {OnlinePlayerCount}.", onlinePlayerCountStr);
				return null;
			}
			
			logger.Verbose("Detected {OnlinePlayerCount} online player(s).", onlinePlayerCount);
			return onlinePlayerCount;
		} finally {
			ArrayPool<byte>.Shared.Return(messageBuffer);
		}
	}
}
