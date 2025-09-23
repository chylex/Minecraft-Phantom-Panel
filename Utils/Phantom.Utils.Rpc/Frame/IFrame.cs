using System.Net.Sockets;
using Phantom.Utils.Rpc.Frame.Types;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Frame;

interface IFrame {
	private const byte TypeSessionTerminationId = 0;
	private const byte TypePingId = 1;
	private const byte TypePongId = 2;
	private const byte TypeMessageId = 3;
	private const byte TypeReplyId = 4;
	private const byte TypeErrorId = 5;
	
	static readonly ReadOnlyMemory<byte> TypeSessionTermination = new ([TypeSessionTerminationId]);
	static readonly ReadOnlyMemory<byte> TypePing = new ([TypePingId]);
	static readonly ReadOnlyMemory<byte> TypePong = new ([TypePongId]);
	static readonly ReadOnlyMemory<byte> TypeMessage = new ([TypeMessageId]);
	static readonly ReadOnlyMemory<byte> TypeReply = new ([TypeReplyId]);
	static readonly ReadOnlyMemory<byte> TypeError = new ([TypeErrorId]);
	
	/// <summary>
	/// Continuously reads frames from the <paramref name="stream"/>.
	/// The task completes normally if the other side sends a <see cref="SessionTerminationFrame"/>. Otherwise, it can only complete exceptionally.
	/// </summary>
	internal static async Task ReadFrom(RpcStream stream, IFrameReader reader, CancellationToken cancellationToken) {
		byte[] oneByteBuffer = new byte[1];
		
		while (true) {
			try {
				await stream.ReadBytes(oneByteBuffer, cancellationToken);
			} catch (IOException e) {
				if (e.InnerException is SocketException socketException) {
					throw socketException;
				}
				else {
					throw;
				}
			}
			
			switch (oneByteBuffer[0]) {
				case TypeSessionTerminationId:
					reader.OnSessionTerminationFrame();
					return;
				
				case TypePingId:
					var pingTime = await PingFrame.Read(stream, cancellationToken);
					await reader.OnPingFrame(pingTime, cancellationToken);
					break;
				
				case TypePongId:
					var pongFrame = await PongFrame.Read(stream, cancellationToken);
					reader.OnPongFrame(pongFrame);
					break;
				
				case TypeMessageId:
					var messageFrame = await MessageFrame.Read(stream, cancellationToken);
					await reader.OnMessageFrame(messageFrame, cancellationToken);
					break;
				
				case TypeReplyId:
					var messageReplyFrame = await MessageReplyFrame.Read(stream, cancellationToken);
					reader.OnMessageReplyFrame(messageReplyFrame);
					break;
				
				case TypeErrorId:
					var messageErrorFrame = await MessageErrorFrame.Read(stream, cancellationToken);
					reader.OnMessageErrorFrame(messageErrorFrame);
					break;
				
				default:
					reader.OnUnknownFrame(oneByteBuffer[0]);
					break;
			}
		}
	}
	
	ReadOnlyMemory<byte> FrameType { get; }
	
	Task Write(RpcStream stream, CancellationToken cancellationToken = default);
}
