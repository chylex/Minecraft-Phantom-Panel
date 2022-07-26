﻿using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Phantom.Common.Logging;
using Phantom.Server.Services;
using Phantom.Utils.Runtime;
using Serilog;

namespace Phantom.Server.Web.Identity.Authentication; 

public sealed class PhantomLoginStore {
	private static readonly ILogger Logger = PhantomLogger.Create<PhantomLoginStore>();
	private static readonly TimeSpan ExpirationTime = TimeSpan.FromMinutes(1);
	
	private readonly ConcurrentDictionary<string, LoginEntry> loginEntries = new ();
	private readonly CancellationToken cancellationToken;

	public PhantomLoginStore(ServiceConfiguration configuration, TaskManager taskManager) {
		this.cancellationToken = configuration.CancellationToken;
		taskManager.Run(RunExpirationLoop);
	}
	
	private async Task RunExpirationLoop() {
		try {
			while (true) {
				await Task.Delay(ExpirationTime, cancellationToken);

				foreach (var (token, entry) in loginEntries) {
					if (entry.IsExpired) {
						Logger.Verbose("Expired login entry for {Username}.", entry.User.UserName);
						loginEntries.TryRemove(token, out _);
					}
				}
			}
		} finally {
			Logger.Information("Expiration loop stopped.");
		}
	}

	internal void Add(string token, IdentityUser user, string password, string returnUrl) {
		loginEntries[token] = new LoginEntry(user, password, returnUrl, Stopwatch.StartNew());
	}

	internal LoginEntry? Pop(string token) {
		if (!loginEntries.TryRemove(token, out var entry)) {
			return null;
		}

		if (entry.IsExpired) {
			Logger.Verbose("Expired login entry for {Username}.", entry.User.UserName);
			return null;
		}
		
		return entry;
	}

	internal sealed record LoginEntry(IdentityUser User, string Password, string ReturnUrl, Stopwatch AddedTime) {
		public bool IsExpired => AddedTime.Elapsed >= ExpirationTime;
	}
}
