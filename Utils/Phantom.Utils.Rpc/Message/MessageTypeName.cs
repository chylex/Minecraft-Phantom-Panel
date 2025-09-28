using System.Text;
using Phantom.Utils.Rpc.Runtime;

namespace Phantom.Utils.Rpc.Message;

sealed class MessageTypeName {
	private readonly string stringValue;
	private readonly ReadOnlyMemory<byte> serializedBytes;
	
	public MessageTypeName(string name) {
		this.stringValue = name;
		this.serializedBytes = Encoding.ASCII.GetBytes(name);
		
		if (serializedBytes.Length is 0 or > byte.MaxValue) {
			throw new ArgumentOutOfRangeException(nameof(name), "Message name must be between 0 and " + byte.MaxValue + " bytes.");
		}
	}
	
	private MessageTypeName(ReadOnlyMemory<byte> serializedBytes) {
		this.stringValue = Encoding.ASCII.GetString(serializedBytes.Span);
		this.serializedBytes = serializedBytes;
	}
	
	public async ValueTask Write(RpcStream stream, CancellationToken cancellationToken) {
		await stream.WriteByte((byte) serializedBytes.Length, cancellationToken);
		await stream.WriteBytes(serializedBytes, cancellationToken);
	}
	
	public static async ValueTask WriteEnd(RpcStream stream, CancellationToken cancellationToken) {
		await stream.WriteByte(value: 0, cancellationToken);
	}
	
	public static async ValueTask<MessageTypeName?> Read(RpcStream stream, CancellationToken cancellationToken) {
		byte serializedBytesLength = await stream.ReadByte(cancellationToken);
		if (serializedBytesLength == 0) {
			return null;
		}
		
		var serializedBytes = await stream.ReadBytes(serializedBytesLength, cancellationToken);
		return new MessageTypeName(serializedBytes);
	}
	
	public override bool Equals(object? obj) {
		if (ReferenceEquals(this, obj)) {
			return true;
		}
		
		return obj is MessageTypeName other && stringValue == other.stringValue;
	}
	
	public override int GetHashCode() {
		return stringValue.GetHashCode();
	}
	
	public override string ToString() {
		return stringValue;
	}
}
