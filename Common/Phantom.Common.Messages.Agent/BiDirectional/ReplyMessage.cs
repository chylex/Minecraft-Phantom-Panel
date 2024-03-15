using MemoryPack;
using Phantom.Utils.Rpc.Message;

namespace Phantom.Common.Messages.Agent.BiDirectional;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record ReplyMessage(
	[property: MemoryPackOrder(0)] uint SequenceId,
	[property: MemoryPackOrder(1)] byte[] SerializedReply
) : IMessageToController, IMessageToAgent, IReply;
