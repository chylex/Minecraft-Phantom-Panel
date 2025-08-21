using System.Text;
using Kajabity.Tools.Java;

namespace Phantom.Agent.Minecraft.Java;

sealed class JavaPropertiesFileEditor {
	private static readonly Encoding Encoding = Encoding.GetEncoding("ISO-8859-1");
	
	private readonly Dictionary<string, string> overriddenProperties = new ();
	
	public void Set(string key, string value) {
		overriddenProperties[key] = value;
	}
	
	public async Task EditOrCreate(string filePath) {
		if (File.Exists(filePath)) {
			string tmpFilePath = filePath + ".tmp";
			File.Copy(filePath, tmpFilePath, overwrite: true);
			await EditFromCopyOrCreate(filePath, tmpFilePath);
			File.Move(tmpFilePath, filePath, overwrite: true);
		}
		else {
			await EditFromCopyOrCreate(null, filePath);
		}
	}
	
	private async Task EditFromCopyOrCreate(string? sourceFilePath, string targetFilePath) {
		var properties = new JavaProperties();
		
		if (sourceFilePath != null) {
			// TODO replace with custom async parser
			await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			properties.Load(sourceStream, Encoding);
		}
		
		foreach (var (key, value) in overriddenProperties) {
			properties[key] = value;
		}
		
		await using var targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
		await using var targetWriter = new StreamWriter(targetStream, Encoding);
		
		await targetWriter.WriteLineAsync("# Properties");
		
		foreach (var (key, value) in properties) {
			await WriteProperty(targetWriter, key, value);
		}
	}
	
	private static async Task WriteProperty(StreamWriter writer, string key, string value) {
		await WritePropertyComponent(writer, key, escapeSpaces: true);
		await writer.WriteAsync('=');
		await WritePropertyComponent(writer, value, escapeSpaces: false);
		await writer.WriteLineAsync();
	}
	
	private static async Task WritePropertyComponent(TextWriter writer, string component, bool escapeSpaces) {
		for (int index = 0; index < component.Length; index++) {
			var c = component[index];
			switch (c) {
				case '\\':
				case '#':
				case '!':
				case '=':
				case ':':
				case ' ' when escapeSpaces || index == 0:
					await writer.WriteAsync('\\');
					await writer.WriteAsync(c);
					break;
				case var _ when c > 31 && c < 127:
					await writer.WriteAsync(c);
					break;
				case '\t':
					await writer.WriteAsync("\\t");
					break;
				case '\n':
					await writer.WriteAsync("\\n");
					break;
				case '\r':
					await writer.WriteAsync("\\r");
					break;
				case '\f':
					await writer.WriteAsync("\\f");
					break;
				default:
					await writer.WriteAsync("\\u");
					await writer.WriteAsync(((int) c).ToString("X4"));
					break;
			}
		}
	}
}
