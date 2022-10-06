using MessagePack;

namespace Phantom.Common.Data.Agent;

[MessagePackObject]
public sealed record AgentInfo(
	[property: Key(0)] Guid Guid,
	[property: Key(1)] string Name,
	[property: Key(2)] ushort Version
);
