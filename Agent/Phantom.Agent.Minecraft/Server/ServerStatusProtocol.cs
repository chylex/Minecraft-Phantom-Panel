using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Phantom.Agent.Minecraft.Server;

public static class ServerStatusProtocol {
	public static async Task<int> GetOnlinePlayerCount(ushort serverPort, CancellationToken cancellationToken) {
		using var tcpClient = new TcpClient();
		await tcpClient.ConnectAsync(IPAddress.Loopback, serverPort, cancellationToken);
		var tcpStream = tcpClient.GetStream();
		
		// https://wiki.vg/Server_List_Ping
		tcpStream.WriteByte(0xFE);
		await tcpStream.FlushAsync(cancellationToken);

		short messageLength = await ReadStreamHeader(tcpStream, cancellationToken);
		return await ReadOnlinePlayerCount(tcpStream, messageLength * 2, cancellationToken);
	}

	private static async Task<short> ReadStreamHeader(NetworkStream tcpStream, CancellationToken cancellationToken) {
		var headerBuffer = ArrayPool<byte>.Shared.Rent(3);
		try {
			await tcpStream.ReadExactlyAsync(headerBuffer, 0, 3, cancellationToken);

			if (headerBuffer[0] != 0xFF) {
				throw new ProtocolException("Unexpected first byte in response from server: " + headerBuffer[0]);
			}

			short messageLength = BinaryPrimitives.ReadInt16BigEndian(headerBuffer.AsSpan(1));
			if (messageLength <= 0) {
				throw new ProtocolException("Unexpected message length in response from server: " + messageLength);
			}
			
			return messageLength;
		} finally {
			ArrayPool<byte>.Shared.Return(headerBuffer);
		}
	}

	private static async Task<int> ReadOnlinePlayerCount(NetworkStream tcpStream, int messageLength, CancellationToken cancellationToken) {
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
				throw new ProtocolException("Could not find message separators in response from server.");
			}

			string onlinePlayerCountStr = Encoding.BigEndianUnicode.GetString(messageBuffer.AsSpan((separator1 + 1)..(separator2 - 1)));
			if (!int.TryParse(onlinePlayerCountStr, out int onlinePlayerCount)) {
				throw new ProtocolException("Could not parse online player count in response from server: " + onlinePlayerCountStr);
			}
			
			return onlinePlayerCount;
		} finally {
			ArrayPool<byte>.Shared.Return(messageBuffer);
		}
	}
	
	public sealed class ProtocolException : Exception {
		internal ProtocolException(string message) : base(message) {}
	}
}
