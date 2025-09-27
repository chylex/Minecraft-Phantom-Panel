using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Phantom.Common.Data.Instance;

namespace Phantom.Agent.Minecraft.Server;

public static class ServerStatusProtocol {
	public static async Task<InstancePlayerCounts> GetPlayerCounts(ushort serverPort, CancellationToken cancellationToken) {
		using var tcpClient = new TcpClient();
		await tcpClient.ConnectAsync(IPAddress.Loopback, serverPort, cancellationToken);
		var tcpStream = tcpClient.GetStream();
		
		// https://wiki.vg/Server_List_Ping
		tcpStream.WriteByte(0xFE);
		await tcpStream.FlushAsync(cancellationToken);
		
		short messageLength = await ReadStreamHeader(tcpStream, cancellationToken);
		return await ReadPlayerCounts(tcpStream, messageLength * 2, cancellationToken);
	}
	
	private static async Task<short> ReadStreamHeader(NetworkStream tcpStream, CancellationToken cancellationToken) {
		var headerBuffer = ArrayPool<byte>.Shared.Rent(3);
		try {
			await tcpStream.ReadExactlyAsync(headerBuffer, offset: 0, count: 3, cancellationToken);
			
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
	
	private static async Task<InstancePlayerCounts> ReadPlayerCounts(NetworkStream tcpStream, int messageLength, CancellationToken cancellationToken) {
		var messageBuffer = ArrayPool<byte>.Shared.Rent(messageLength);
		try {
			await tcpStream.ReadExactlyAsync(messageBuffer, offset: 0, messageLength, cancellationToken);
			return ReadPlayerCountsFromResponse(messageBuffer.AsSpan(start: 0, messageLength));
		} finally {
			ArrayPool<byte>.Shared.Return(messageBuffer);
		}
	}
	
	/// <summary>
	/// Legacy query protocol uses the paragraph symbol (§) as separator encoded in UTF-16BE.
	/// </summary>
	private static readonly byte[] Separator = [0x00, 0xA7];
	
	private static InstancePlayerCounts ReadPlayerCountsFromResponse(ReadOnlySpan<byte> messageBuffer) {
		int lastSeparator = messageBuffer.LastIndexOf(Separator);
		int middleSeparator = messageBuffer[..lastSeparator].LastIndexOf(Separator);
		
		if (lastSeparator == -1 || middleSeparator == -1) {
			throw new ProtocolException("Could not find message separators in response from server.");
		}
		
		var onlinePlayerCountBuffer = messageBuffer[(middleSeparator + Separator.Length)..lastSeparator];
		var maximumPlayerCountBuffer = messageBuffer[(lastSeparator + Separator.Length)..];
		
		// Player counts are integers, whose maximum string length is 10 characters.
		Span<char> integerStringBuffer = stackalloc char[10];
		
		return new InstancePlayerCounts(
			DecodeAndParsePlayerCount(onlinePlayerCountBuffer, integerStringBuffer, "online"),
			DecodeAndParsePlayerCount(maximumPlayerCountBuffer, integerStringBuffer, "maximum")
		);
	}
	
	private static int DecodeAndParsePlayerCount(ReadOnlySpan<byte> inputBuffer, Span<char> tempCharBuffer, string countType) {
		if (!Encoding.BigEndianUnicode.TryGetChars(inputBuffer, tempCharBuffer, out int charCount)) {
			throw new ProtocolException("Could not decode " + countType + " player count in response from server.");
		}
		
		if (!int.TryParse(tempCharBuffer, out int playerCount)) {
			throw new ProtocolException("Could not parse " + countType + " player count in response from server: " + tempCharBuffer[..charCount].ToString());
		}
		
		return playerCount;
	}
	
	public sealed class ProtocolException : Exception {
		internal ProtocolException(string message) : base(message) {}
	}
}
