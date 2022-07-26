﻿using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Phantom.Common.Data.Minecraft;
using Phantom.Common.Logging;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Common.Minecraft;

public sealed class MinecraftVersions : IDisposable {
	private static readonly ILogger Logger = PhantomLogger.Create<MinecraftVersions>();
	private static readonly TimeSpan CacheRetentionTime = TimeSpan.FromMinutes(10);

	private const string VersionManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";

	private readonly HttpClient http = new ();

	private readonly Stopwatch cacheTimer = new ();
	private readonly SemaphoreSlim cachedVersionsSemaphore = new (1, 1);

	private ImmutableArray<MinecraftVersion>? cachedVersions;
	private ImmutableArray<MinecraftVersion>? CachedVersionsUnlessExpired => cacheTimer.IsRunning && cacheTimer.Elapsed < CacheRetentionTime ? cachedVersions : null;

	public void Dispose() {
		http.Dispose();
		cachedVersionsSemaphore.Dispose();
	}

	public async Task<ImmutableArray<MinecraftVersion>> GetVersions(CancellationToken cancellationToken) {
		if (CachedVersionsUnlessExpired is {} earlyResult) {
			return earlyResult;
		}

		try {
			await cachedVersionsSemaphore.WaitAsync(cancellationToken);
		} catch (OperationCanceledException) {
			return ImmutableArray<MinecraftVersion>.Empty;
		}

		try {
			if (CachedVersionsUnlessExpired is {} racedResult) {
				return racedResult;
			}

			ImmutableArray<MinecraftVersion> versions = await FetchVersions(cancellationToken) ?? ImmutableArray<MinecraftVersion>.Empty;
			Logger.Information("Refreshed Minecraft version cache, {Versions} version(s) found.", versions.Length);
			
			cachedVersions = versions;
			cacheTimer.Restart();
			return versions;
		} finally {
			cachedVersionsSemaphore.Release();
		}
	}

	private async Task<ImmutableArray<MinecraftVersion>?> FetchVersions(CancellationToken cancellationToken) {
		return await FetchOrFailSilently(async () => {
			var versionManifest = await FetchJson(http, VersionManifestUrl, "version manifest", cancellationToken);
			return GetVersionsFromManifest(versionManifest);
		});
	}

	public async Task<MinecraftServerExecutableInfo?> GetServerExecutableInfo(string version, CancellationToken cancellationToken) {
		return await FetchOrFailSilently(async () => {
			var versions = await GetVersions(cancellationToken);
			var versionObject = versions.FirstOrDefault(v => v.Id == version);
			if (versionObject == null) {
				Logger.Error("Version {Version} was not found in version manifest.", version);
				return null;
			}

			var versionMetadata = await FetchJson(http, versionObject.MetadataUrl, "version metadata", cancellationToken);
			return GetServerExecutableInfoFromMetadata(versionMetadata);
		});
	}

	private static async Task<T?> FetchOrFailSilently<T>(Func<Task<T?>> task) {
		try {
			return await task();
		} catch (OperationCanceledException) {
			return default;
		} catch (StopProcedureException) {
			return default;
		} catch (Exception e) {
			Logger.Error(e, "An unexpected error occurred.");
			return default;
		}
	}

	private static async Task<JsonElement> FetchJson(HttpClient http, string url, string description, CancellationToken cancellationToken) {
		Logger.Information("Fetching {Description} JSON from: {Url}", description, url);

		try {
			return await http.GetFromJsonAsync<JsonElement>(url, cancellationToken);
		} catch (OperationCanceledException) {
			throw StopProcedureException.Instance;
		}  catch (HttpRequestException e) {
			Logger.Error(e, "Unable to download {Description}.", description);
			throw StopProcedureException.Instance;
		} catch (Exception e) {
			Logger.Error(e, "Unable to parse {Description} as JSON.", description);
			throw StopProcedureException.Instance;
		}
	}

	private static ImmutableArray<MinecraftVersion> GetVersionsFromManifest(JsonElement versionManifest) {
		JsonElement versionsElement = GetJsonPropertyOrThrow(versionManifest, "versions", JsonValueKind.Array, "version manifest");
		var foundVersions = ImmutableArray.CreateBuilder<MinecraftVersion>(versionsElement.GetArrayLength());

		foreach (var versionElement in versionsElement.EnumerateArray()) {
			try {
				foundVersions.Add(GetVersionFromManifestEntry(versionElement));
			} catch (StopProcedureException) {}
		}

		return foundVersions.ToImmutable();
	}

	private static MinecraftVersion GetVersionFromManifestEntry(JsonElement versionElement) {
		JsonElement idElement = GetJsonPropertyOrThrow(versionElement, "id", JsonValueKind.String, "version entry in version manifest");
		string id = idElement.GetString() ?? throw new InvalidOperationException();

		JsonElement typeElement = GetJsonPropertyOrThrow(versionElement, "type", JsonValueKind.String, "version entry in version manifest");
		string? typeString = typeElement.GetString();

		var type = MinecraftVersionTypes.FromString(typeString);
		if (type == MinecraftVersionType.Other) {
			Logger.Verbose("Unknown version type: {Type} ({Version})", typeString, id);
		}

		JsonElement urlElement = GetJsonPropertyOrThrow(versionElement, "url", JsonValueKind.String, "version entry in version manifest");
		string? url = urlElement.GetString();

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
			Logger.Error("The \"url\" key in version entry in version manifest does not contain a valid URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		if (uri.Scheme != "https" || !uri.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
			Logger.Error("The \"url\" key in version entry in version manifest does not contain an accepted URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		return new MinecraftVersion(id, type, url);
	}

	private static MinecraftServerExecutableInfo GetServerExecutableInfoFromMetadata(JsonElement versionMetadata) {
		JsonElement downloadsElement = GetJsonPropertyOrThrow(versionMetadata, "downloads", JsonValueKind.Object, "version metadata");
		JsonElement serverElement = GetJsonPropertyOrThrow(downloadsElement, "server", JsonValueKind.Object, "downloads object in version metadata");
		JsonElement urlElement = GetJsonPropertyOrThrow(serverElement, "url", JsonValueKind.String, "downloads.server object in version metadata");
		string? url = urlElement.GetString();

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
			Logger.Error("The \"url\" key in downloads.server object in version metadata does not contain a valid URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		if (uri.Scheme != "https" || !uri.AbsolutePath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) {
			Logger.Error("The \"url\" key in downloads.server object in version metadata does not contain a accepted URL: {Url}", url);
			throw StopProcedureException.Instance;
		}

		JsonElement sizeElement = GetJsonPropertyOrThrow(serverElement, "size", JsonValueKind.Number, "downloads.server object in version metadata");
		ulong size;
		try {
			size = sizeElement.GetUInt64();
		} catch (FormatException) {
			Logger.Error("The \"size\" key in downloads.server object in version metadata contains an invalid file size: {Size}", sizeElement);
			throw StopProcedureException.Instance;
		}

		JsonElement sha1Element = GetJsonPropertyOrThrow(serverElement, "sha1", JsonValueKind.String, "downloads.server object in version metadata");
		Sha1String hash;
		try {
			hash = Sha1String.FromString(sha1Element.GetString());
		} catch (Exception) {
			Logger.Error("The \"sha1\" key in downloads.server object in version metadata does not contain a valid SHA-1 hash: {Sha1}", sha1Element.GetString());
			throw StopProcedureException.Instance;
		}

		return new MinecraftServerExecutableInfo(url, hash, new FileSize(size));
	}

	private static JsonElement GetJsonPropertyOrThrow(JsonElement parentElement, string propertyKey, JsonValueKind expectedKind, string location) {
		if (!parentElement.TryGetProperty(propertyKey, out var valueElement)) {
			Logger.Error("Missing \"{Property}\" key in " + location + ".", propertyKey);
			throw StopProcedureException.Instance;
		}

		if (valueElement.ValueKind != expectedKind) {
			Logger.Error("The \"{Property}\" key in " + location + " does not contain a JSON {ExpectedType}. Actual type: {ActualType}", propertyKey, expectedKind, valueElement.ValueKind);
			throw StopProcedureException.Instance;
		}

		return valueElement;
	}
}
