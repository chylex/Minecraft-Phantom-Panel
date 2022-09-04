using MessagePack;

namespace Phantom.Common.Data.Java; 

[MessagePackObject]
public sealed record TaggedJavaRuntime(
	[property: Key(0)] Guid Guid,
	[property: Key(1)] JavaRuntime Runtime
);
