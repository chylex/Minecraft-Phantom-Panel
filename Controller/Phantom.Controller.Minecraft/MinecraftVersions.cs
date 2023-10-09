using System.Collections.Immutable;
using System.Diagnostics;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Logging;
using Serilog;

namespace Phantom.Controller.Minecraft;

public sealed class MinecraftVersions : IDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftVersions>();
	private static readonly TimeSpan CacheRetentionTime = TimeSpan.FromMinutes(10);

	private readonly MinecraftVersionApi api = new ();
	private readonly Stopwatch cacheTimer = new ();
	private readonly SemaphoreSlim cacheSemaphore = new (1, 1);

	private bool IsCacheNotExpired => cacheTimer.IsRunning && cacheTimer.Elapsed < CacheRetentionTime;
	
	private ImmutableArray<MinecraftVersion>? cachedVersions;
	private readonly Dictionary<string, FileDownloadInfo?> cachedServerExecutables = new ();

	public void Dispose() {
		api.Dispose();
		cacheSemaphore.Dispose();
	}

	public async Task<ImmutableArray<MinecraftVersion>> GetVersions(CancellationToken cancellationToken) {
		return await GetCachedObject(() => cachedVersions != null, () => cachedVersions.GetValueOrDefault(), v => cachedVersions = v, LoadVersions, cancellationToken);
	}

	private async Task<ImmutableArray<MinecraftVersion>> LoadVersions(CancellationToken cancellationToken) {
		ImmutableArray<MinecraftVersion> versions = await api.GetVersions(cancellationToken);
		Logger.Information("Refreshed Minecraft version cache, {Versions} version(s) found.", versions.Length);
		return versions;
	}

	public async Task<FileDownloadInfo?> GetServerExecutableInfo(string version, CancellationToken cancellationToken) {
		var versions = await GetVersions(cancellationToken);
		return await GetCachedObject(() => cachedServerExecutables.ContainsKey(version), () => cachedServerExecutables[version], v => cachedServerExecutables[version] = v, ct => LoadServerExecutableInfo(versions, version, ct), cancellationToken);
	}

	private async Task<FileDownloadInfo?> LoadServerExecutableInfo(ImmutableArray<MinecraftVersion> versions, string version, CancellationToken cancellationToken) {
		var info = await api.GetServerExecutableInfo(versions, version, cancellationToken);
			
		if (info == null) {
			Logger.Information("Refreshed Minecraft {Version} server executable cache, no file found.", version);
		}
		else {
			Logger.Information("Refreshed Minecraft {Version} server executable cache, found file: {Url}.", version, info.DownloadUrl);
		}

		return info;
	}

	private async Task<T> GetCachedObject<T>(Func<bool> isLoaded, Func<T> fieldGetter, Action<T> fieldSetter, Func<CancellationToken, Task<T>> fieldLoader, CancellationToken cancellationToken) {
		if (IsCacheNotExpired && isLoaded()) {
			return fieldGetter();
		}

		await cacheSemaphore.WaitAsync(cancellationToken);
		try {
			if (IsCacheNotExpired && isLoaded()) {
				return fieldGetter();
			}

			T result = await fieldLoader(cancellationToken);
			fieldSetter(result);
			
			cacheTimer.Restart();
			return result;
		} finally {
			cacheSemaphore.Release();
		}
	}
}
