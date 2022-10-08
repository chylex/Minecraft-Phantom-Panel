namespace Phantom.Agent.Minecraft.Command; 

public static class MinecraftCommand {
	public const string Stop = "stop";

	public static string Say(string message) {
		return "say " + message;
	}
}
