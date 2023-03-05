using Phantom.Agent.Minecraft.Java;

namespace Phantom.Agent.Minecraft.Properties;

public sealed record ServerProperties(
	ushort ServerPort,
	ushort RconPort,
	bool EnableRcon = true
) {
	internal void SetTo(JavaPropertiesFileEditor properties) {
		MinecraftServerProperties.ServerPort.Set(properties, ServerPort);
		MinecraftServerProperties.RconPort.Set(properties, RconPort);
		MinecraftServerProperties.EnableRcon.Set(properties, EnableRcon);
	}
}
