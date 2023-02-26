using MemoryPack;
using Phantom.Common.Data.Minecraft;

namespace Phantom.Common.Data.Instance; 

[MemoryPackable]
public sealed partial record InstanceLaunchProperties(
	[property: MemoryPackOrder(0)] FileDownloadInfo? ServerDownloadInfo
);
