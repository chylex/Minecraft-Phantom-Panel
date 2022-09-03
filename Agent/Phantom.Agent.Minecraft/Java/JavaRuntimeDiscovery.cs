using System.Diagnostics;
using Phantom.Common.Data.Java;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Agent.Minecraft.Java;

public static class JavaRuntimeDiscovery {
	private static readonly ILogger Logger = PhantomLogger.Create(typeof(JavaRuntimeDiscovery));

	public static string? GetSystemSearchPath() {
		const string LinuxJavaPath = "/usr/lib/jvm";

		if (OperatingSystem.IsLinux() && Directory.Exists(LinuxJavaPath)) {
			return LinuxJavaPath;
		}

		return null;
	}

	public static async IAsyncEnumerable<JavaRuntime> Scan(string folderPath) {
		Logger.Information("Starting Java runtime scan in: {FolderPath}", folderPath);

		string javaExecutableName = OperatingSystem.IsWindows() ? "java.exe" : "java";

		foreach (var binFolderPath in Directory.EnumerateDirectories(folderPath, "bin", new EnumerationOptions {
			MatchType = MatchType.Simple,
			RecurseSubdirectories = true,
			ReturnSpecialDirectories = false,
			IgnoreInaccessible = true
		})) {
			var javaExecutablePath = Path.Combine(binFolderPath, javaExecutableName);
			if (File.Exists(javaExecutablePath)) {
				Logger.Information("Found candidate Java executable: {JavaExecutablePath}", javaExecutablePath);

				JavaVersion? foundVersion;
				try {
					foundVersion = await TryReadJavaVersionInformationFromProcess(javaExecutablePath);
				} catch (OperationCanceledException) {
					Logger.Error("Java process did not exit in time.");
					continue;
				} catch (Exception e) {
					Logger.Error(e, "Caught exception while reading Java version information.");
					continue;
				}

				if (foundVersion == null) {
					Logger.Error("Java executable did not output version information.");
					continue;
				}

				Logger.Information("Found Java {Version} from {Vendor}: {Path}", foundVersion.FullVersion, foundVersion.Vendor, javaExecutablePath);
				yield return new JavaRuntime(javaExecutablePath, foundVersion);
			}
		}
	}

	private static async Task<JavaVersion?> TryReadJavaVersionInformationFromProcess(string javaExecutablePath) {
		var startInfo = new ProcessStartInfo {
			FileName = javaExecutablePath,
			WorkingDirectory = Path.GetDirectoryName(javaExecutablePath),
			Arguments = "-XshowSettings:properties -version",
			RedirectStandardInput = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = false
		};

		var process = new Process { StartInfo = startInfo };
		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		try {
			process.Start();

			JavaVersionBuilder versionBuilder = new ();

			while (await process.StandardError.ReadLineAsync(cancellationTokenSource.Token) is {} line) {
				ExtractJavaVersionPropertiesFromLine(line, versionBuilder);
				
				JavaVersion? version = versionBuilder.TryBuild();
				if (version != null) {
					return version;
				}
			}

			await process.WaitForExitAsync(cancellationTokenSource.Token);
			return null;
		} finally {
			process.Dispose();
			cancellationTokenSource.Dispose();
		}
	}

	private static void ExtractJavaVersionPropertiesFromLine(ReadOnlySpan<char> line, JavaVersionBuilder versionBuilder) {
		line = line.TrimStart();

		int separatorIndex = line.IndexOf('=');
		if (separatorIndex == -1) {
			return;
		}

		var propertyName = line[..separatorIndex].TrimEnd();
		if (propertyName.Equals("java.specification.version", StringComparison.Ordinal)) {
			versionBuilder.MainVersion = ExtractValue(line, separatorIndex);
		}
		else if (propertyName.Equals("java.version", StringComparison.Ordinal)) {
			versionBuilder.FullVersion = ExtractValue(line, separatorIndex);
		}
		else if (propertyName.Equals("java.vm.vendor", StringComparison.Ordinal)) {
			versionBuilder.Vendor = ExtractValue(line, separatorIndex);
		}
	}

	private static string ExtractValue(ReadOnlySpan<char> line, int separatorIndex) {
		return line[(separatorIndex + 1)..].Trim().ToString();
	}

	private sealed class JavaVersionBuilder {
		public string? MainVersion { get; set; } = null;
		public string? FullVersion { get; set; } = null;
		public string? Vendor { get; set; } = null;

		public JavaVersion? TryBuild() {
			if (MainVersion == null || FullVersion == null || Vendor == null) {
				return null;
			}
			else {
				return new JavaVersion(MainVersion, FullVersion, Vendor);
			}
		}
	}
}
