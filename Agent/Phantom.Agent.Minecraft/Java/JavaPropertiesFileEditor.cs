namespace Phantom.Agent.Minecraft.Java;

sealed class JavaPropertiesFileEditor {
	private readonly Dictionary<string, string> overriddenProperties = new ();
	
	public void Set(string key, string value) {
		overriddenProperties[key] = value;
	}
	
	public async Task EditOrCreate(string filePath, string comment, CancellationToken cancellationToken) {
		if (File.Exists(filePath)) {
			string tmpFilePath = filePath + ".tmp";
			await Edit(filePath, tmpFilePath, comment, cancellationToken);
			File.Move(tmpFilePath, filePath, overwrite: true);
		}
		else {
			await Create(filePath, comment, cancellationToken);
		}
	}
	
	private async Task Create(string targetFilePath, string comment, CancellationToken cancellationToken) {
		await using var targetWriter = new JavaPropertiesStream.Writer(targetFilePath);
		
		await targetWriter.WriteComment(comment, cancellationToken);
		
		foreach ((string key, string value) in overriddenProperties) {
			await targetWriter.WriteProperty(key, value, cancellationToken);
		}
	}
	
	private async Task Edit(string sourceFilePath, string targetFilePath, string comment, CancellationToken cancellationToken) {
		using var sourceReader = new JavaPropertiesStream.Reader(sourceFilePath);
		await using var targetWriter = new JavaPropertiesStream.Writer(targetFilePath);
		
		await targetWriter.WriteComment(comment, cancellationToken);
		
		var remainingOverriddenPropertyKeys = new HashSet<string>(overriddenProperties.Keys);
		
		await foreach ((string key, string value) in sourceReader.ReadProperties(cancellationToken)) {
			if (remainingOverriddenPropertyKeys.Remove(key)) {
				await targetWriter.WriteProperty(key, overriddenProperties[key], cancellationToken);
			}
			else {
				await targetWriter.WriteProperty(key, value, cancellationToken);
			}
		}
		
		foreach (string key in remainingOverriddenPropertyKeys) {
			await targetWriter.WriteProperty(key, overriddenProperties[key], cancellationToken);
		}
	}
}
