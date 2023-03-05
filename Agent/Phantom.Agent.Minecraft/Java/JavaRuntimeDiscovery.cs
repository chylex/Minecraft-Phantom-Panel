using System.Diagnostics;
using Phantom.Common.Data.Java;
using Phantom.Common.Logging;
using Phantom.Utils.IO;
using Serilog;

namespace Phantom.Agent.Minecraft.Java;

public sealed class JavaRuntimeDiscovery {
	private static readonly ILogger Logger = PhantomLogger.Create(nameof(JavaRuntimeDiscovery));

	public static string? GetSystemSearchPath() {
		const string LinuxJavaPath = "/usr/lib/jvm";

		if (OperatingSystem.IsLinux() && Directory.Exists(LinuxJavaPath)) {
			return LinuxJavaPath;
		}

		return null;
	}

	public static IAsyncEnumerable<JavaRuntimeExecutable> Scan(string folderPath) {
		return new JavaRuntimeDiscovery().ScanInternal(folderPath);
	}
	
	private readonly Dictionary<string, int> duplicateDisplayNames = new ();
	
	private async IAsyncEnumerable<JavaRuntimeExecutable> ScanInternal(string folderPath) {
		Logger.Information("Starting Java runtime scan in: {FolderPath}", folderPath);

		string javaExecutableName = OperatingSystem.IsWindows() ? "java.exe" : "java";

		foreach (var binFolderPath in Directory.EnumerateDirectories(Paths.ExpandTilde(folderPath), "bin", new EnumerationOptions {
			MatchType = MatchType.Simple,
			RecurseSubdirectories = true,
			ReturnSpecialDirectories = false,
			IgnoreInaccessible = true,
			AttributesToSkip = FileAttributes.Hidden | FileAttributes.ReparsePoint | FileAttributes.System
		}).Order()) {
			var javaExecutablePath = Paths.NormalizeSlashes(Path.Combine(binFolderPath, javaExecutableName));
			
			FileAttributes javaExecutableAttributes;
			try {
				javaExecutableAttributes = File.GetAttributes(javaExecutablePath);
			} catch (Exception) {
				continue;
			}

			if (javaExecutableAttributes.HasFlag(FileAttributes.ReparsePoint)) {
				continue;
			}

			Logger.Information("Found candidate Java executable: {JavaExecutablePath}", javaExecutablePath);

			JavaRuntime? foundRuntime;
			try {
				foundRuntime = await TryReadJavaRuntimeInformationFromProcess(javaExecutablePath);
			} catch (OperationCanceledException) {
				Logger.Error("Java process did not exit in time.");
				continue;
			} catch (Exception e) {
				Logger.Error(e, "Caught exception while reading Java version information.");
				continue;
			}

			if (foundRuntime == null) {
				Logger.Error("Java executable did not output version information.");
				continue;
			}

			Logger.Information("Found Java {DisplayName} at: {Path}", foundRuntime.DisplayName, javaExecutablePath);
			yield return new JavaRuntimeExecutable(javaExecutablePath, foundRuntime);
		}
	}

	private async Task<JavaRuntime?> TryReadJavaRuntimeInformationFromProcess(string javaExecutablePath) {
		var startInfo = new ProcessStartInfo {
			FileName = javaExecutablePath,
			WorkingDirectory = Path.GetDirectoryName(javaExecutablePath),
			Arguments = "-XshowSettings:properties -version",
			RedirectStandardInput = false,
			RedirectStandardOutput = false,
			RedirectStandardError = true,
			UseShellExecute = false
		};

		var process = new Process { StartInfo = startInfo };
		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		try {
			process.Start();

			JavaRuntimeBuilder runtimeBuilder = new ();

			while (await process.StandardError.ReadLineAsync(cancellationTokenSource.Token) is {} line) {
				ExtractJavaVersionPropertiesFromLine(line, runtimeBuilder);
				
				JavaRuntime? runtime = runtimeBuilder.TryBuild(duplicateDisplayNames);
				if (runtime != null) {
					return runtime;
				}
			}

			await process.WaitForExitAsync(cancellationTokenSource.Token);
			return null;
		} finally {
			process.Dispose();
			cancellationTokenSource.Dispose();
		}
	}

	private static void ExtractJavaVersionPropertiesFromLine(ReadOnlySpan<char> line, JavaRuntimeBuilder runtimeBuilder) {
		line = line.TrimStart();

		int separatorIndex = line.IndexOf('=');
		if (separatorIndex == -1) {
			return;
		}

		var propertyName = line[..separatorIndex].TrimEnd();
		if (propertyName.Equals("java.specification.version", StringComparison.Ordinal)) {
			runtimeBuilder.MainVersion = ExtractValue(line, separatorIndex);
		}
		else if (propertyName.Equals("java.version", StringComparison.Ordinal)) {
			runtimeBuilder.FullVersion = ExtractValue(line, separatorIndex);
		}
		else if (propertyName.Equals("java.vm.vendor", StringComparison.Ordinal)) {
			runtimeBuilder.Vendor = ExtractValue(line, separatorIndex);
		}
	}

	private static string ExtractValue(ReadOnlySpan<char> line, int separatorIndex) {
		return line[(separatorIndex + 1)..].Trim().ToString();
	}

	private sealed class JavaRuntimeBuilder {
		public string? MainVersion { get; set; } = null;
		public string? FullVersion { get; set; } = null;
		public string? Vendor { get; set; } = null;

		public JavaRuntime? TryBuild(Dictionary<string, int> duplicateDisplayNames) {
			if (MainVersion == null || FullVersion == null || Vendor == null) {
				return null;
			}
			else {
				string displayName = $"{FullVersion} ({Vendor})";
				
				if (duplicateDisplayNames.TryGetValue(displayName, out int usedCount)) {
					++usedCount;
					displayName += " (" + usedCount + ")";
					duplicateDisplayNames[displayName] = usedCount;
				}
				else {
					duplicateDisplayNames[displayName] = 1;
				}
				
				return new JavaRuntime(MainVersion, FullVersion, displayName);
			}
		}
	}
}
