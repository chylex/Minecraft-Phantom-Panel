namespace Phantom.Agent.Minecraft.Command; 

public static class MinecraftCommand {
	public const string SaveOn = "save-on";
	public const string SaveOff = "save-off";
	public const string Stop = "stop";
	
	public static string Say(string message) {
		return "say " + message;
	}

	public static string SaveAll(bool flush) {
		return flush ? "save-all flush" : "save-all";
	}
}
