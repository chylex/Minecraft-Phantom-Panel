using System.Collections.Immutable;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Phantom.Utils.Logging;
using ILogger = Serilog.ILogger;

namespace Phantom.Web.Services.Authentication; 

public sealed class UserSessionBrowserStorage {
	private static readonly ILogger Logger = PhantomLogger.Create<UserSessionBrowserStorage>();
	
	private const string SessionTokenKey = "PhantomSession";

	private readonly ProtectedLocalStorage localStorage;
	
	public UserSessionBrowserStorage(ProtectedLocalStorage localStorage) {
		this.localStorage = localStorage;
	}
	
	internal sealed record LocalStorageEntry(Guid UserGuid, ImmutableArray<byte> Token);

	internal async Task<LocalStorageEntry?> Get() {
		try {
			var result = await localStorage.GetAsync<LocalStorageEntry>(SessionTokenKey);
			return result.Success ? result.Value : null;
		} catch (InvalidOperationException) {
			return null;
		} catch (CryptographicException) {
			return null;
		} catch (Exception e) {
			Logger.Error(e, "Could not read local storage entry.");
			return null;
		}
	}

	internal async Task Store(Guid userGuid, ImmutableArray<byte> token) {
		await localStorage.SetAsync(SessionTokenKey, new LocalStorageEntry(userGuid, token));
	}

	internal async Task<LocalStorageEntry?> Delete() {
		var oldEntry = await Get();
		if (oldEntry != null) {
			await localStorage.DeleteAsync(SessionTokenKey);
		}
		
		return oldEntry;
	}
}
