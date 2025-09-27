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
	
	public static void RequireMaximumFileSize(string path, long maximumBytes) {
		var actualBytes = new FileInfo(path).Length;
		if (actualBytes > maximumBytes) {
			throw new IOException("Expected file size to be at most " + maximumBytes + " B, actual size is " + actualBytes + " B.");
		}
	}
}
