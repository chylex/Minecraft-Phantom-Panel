namespace Phantom.Utils.IO;

public static class Paths {
	public static string ExpandTilde(string path) {
		if (path == "~" || path.StartsWith("~/")) {
			return string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), path.AsSpan(1));
		}
		else {
			return path;
		}
	}
	
	public static string NormalizeSlashes(string path) {
		return path.Replace(oldChar: '\\', newChar: '/');
	}
}
