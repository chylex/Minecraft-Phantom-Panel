namespace Phantom.Utils.IO;

public readonly record struct FileSize(ulong Bytes) {
	private const int Scale = 1024;
	
	private static readonly string[] Units = [
		"B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB",
	];
	
	public string ToHumanReadable(int decimalPlaces) {
		int power = Bytes == 0L ? 0 : (int) Math.Log(Bytes, Scale);
		int unit = power >= Units.Length ? Units.Length - 1 : power;
		if (unit == 0) {
			return Bytes + " B";
		}
		
		string format = "{0:n" + decimalPlaces + "} {1}";
		return string.Format(format, Bytes / Math.Pow(Scale, unit), Units[unit]);
	}
}
