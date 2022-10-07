using Kajabity.Tools.Java;

namespace Phantom.Agent.Minecraft.Properties; 

abstract class MinecraftServerProperty<T> {
	private readonly string key;

	protected MinecraftServerProperty(string key) {
		this.key = key;
	}
	
	protected abstract T Read(string value);
	protected abstract string Write(T value);

	public void Set(JavaProperties properties, T value) {
		properties.SetProperty(key, Write(value));
	}
}
