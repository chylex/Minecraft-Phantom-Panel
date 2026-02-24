namespace Phantom.Agent.Minecraft.Command;

public static class MinecraftCommand {
	public const string SaveOn = "save-on";
	public const string SaveOff = "save-off";
	
	public static string SaveAll(bool flush) {
		return flush ? "save-all flush" : "save-all";
	}
}
