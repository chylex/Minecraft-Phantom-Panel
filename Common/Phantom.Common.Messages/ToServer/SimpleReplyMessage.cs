using System.Runtime.CompilerServices;
using MemoryPack;

namespace Phantom.Common.Messages.ToServer;

[MemoryPackable]
public sealed partial record SimpleReplyMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] int EnumValue
) : IMessageToServer {
	public static SimpleReplyMessage FromEnum<TEnum>(uint sequenceId, TEnum enumValue) where TEnum : Enum {
		if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<int>()) {
			throw new ArgumentException("Enum type " + typeof(TEnum).Name + " is not compatible with int.", nameof(TEnum));
		}

		return new SimpleReplyMessage(sequenceId, Unsafe.As<TEnum, int>(ref enumValue));
	}

	public Task Accept(IMessageToServerListener listener) {
		return listener.HandleSimpleReply(this);
	}
}
