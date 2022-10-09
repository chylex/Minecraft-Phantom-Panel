namespace Phantom.Common.Data.Minecraft; 

public sealed record MinecraftVersion(
	string Id,
	MinecraftVersionType Type,
	string MetadataUrl
);
