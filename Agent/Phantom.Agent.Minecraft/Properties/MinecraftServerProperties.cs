namespace Phantom.Agent.Minecraft.Properties;

static class MinecraftServerProperties {
	private sealed class Boolean : MinecraftServerProperty<bool> {
		public Boolean(string key) : base(key) {}
		
		protected override bool Read(string value) => value.Equals("true", StringComparison.OrdinalIgnoreCase);
		protected override string Write(bool value) => value ? "true" : "false";
	}
	
	private sealed class UnsignedShort : MinecraftServerProperty<ushort> {
		public UnsignedShort(string key) : base(key) {}
		
		protected override ushort Read(string value) => ushort.Parse(value);
		protected override string Write(ushort value) => value.ToString();
	}

	public static readonly MinecraftServerProperty<ushort> ServerPort = new UnsignedShort("server-port");
	public static readonly MinecraftServerProperty<ushort> RconPort = new UnsignedShort("rcon.port");
	public static readonly MinecraftServerProperty<bool> EnableRcon = new Boolean("enable-rcon");
}
