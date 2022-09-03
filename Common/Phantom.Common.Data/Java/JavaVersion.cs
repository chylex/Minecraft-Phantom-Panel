using MessagePack;

namespace Phantom.Common.Data.Java; 

[MessagePackObject]
public sealed record JavaVersion(
	[property: Key(0)] string MainVersion,
	[property: Key(1)] string FullVersion,
	[property: Key(2)] string Vendor
);
