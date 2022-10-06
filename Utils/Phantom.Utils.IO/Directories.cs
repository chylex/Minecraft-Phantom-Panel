namespace Phantom.Utils.IO; 

public static class Directories {
	public static void Create(string path, UnixFileMode mode) {
		if (OperatingSystem.IsWindows()) {
			Directory.CreateDirectory(path);
		}
		else {
			Directory.CreateDirectory(path, mode);
		}
	}
}
