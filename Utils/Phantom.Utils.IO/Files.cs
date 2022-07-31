namespace Phantom.Utils.IO;

public static class Files {
	public static async Task WriteBytesAsync(string path, ReadOnlyMemory<byte> bytes, FileMode mode, UnixFileMode chmod) {
		var options = new FileStreamOptions {
			Mode = mode,
			Access = FileAccess.Write,
			Options = FileOptions.Asynchronous,
			Share = FileShare.Read,
		};

		if (!OperatingSystem.IsWindows()) {
			options.UnixCreateMode = chmod;
		}

		await using var stream = new FileStream(path, options);
		await stream.WriteAsync(bytes);
	}
}
