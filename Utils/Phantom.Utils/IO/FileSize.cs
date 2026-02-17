namespace Phantom.Utils.IO;

public readonly record struct FileSize(ulong Bytes) {
	private const int Scale = 1024;
	
	private static readonly string[] Units = [
		"B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB",
	];
	
	public string ToHumanReadable(int decimalPlaces) {
		return ToHumanReadable(Bytes, decimalPlaces);
	}
	
	public static string ToHumanReadable(ulong bytes, int decimalPlaces) {
		int power = bytes == 0L ? 0 : (int) Math.Log(bytes, Scale);
		int unit = power >= Units.Length ? Units.Length - 1 : power;
		if (unit == 0) {
			return bytes + " B";
		}
		
		string format = "{0:n" + decimalPlaces + "} {1}";
		return string.Format(format, bytes / Math.Pow(Scale, unit), Units[unit]);
	}
}
