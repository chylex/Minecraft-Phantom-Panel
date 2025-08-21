using System.Diagnostics.CodeAnalysis;
using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Phantom.Web.Services;

public sealed class Navigation {
	public static Func<IServiceProvider, Navigation> Create(string basePath) {
		return provider => new Navigation(basePath, provider.GetRequiredService<NavigationManager>());
	}
	
	public string BasePath { get; }
	
	private readonly NavigationManager navigationManager;
	
	private Navigation(string basePath, NavigationManager navigationManager) {
		this.BasePath = basePath;
		this.navigationManager = navigationManager;
	}
	
	public bool GetQueryParameter(string key, [MaybeNullWhen(false)] out string value) {
		var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
		var query = HttpUtility.ParseQueryString(uri.Query);
		
		value = query.Get(key);
		return value != null;
	}
	
	public string CreateReturnUrl() {
		return navigationManager.ToBaseRelativePath(navigationManager.Uri).TrimEnd('/');
	}
	
	public async Task NavigateTo(string url, bool forceLoad = false) {
		var newPath = BasePath + url;
		
		var navigationTaskSource = new TaskCompletionSource();
		navigationManager.LocationChanged += NavigationManagerOnLocationChanged;
		try {
			navigationManager.NavigateTo(newPath, forceLoad);
			await navigationTaskSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
		} finally {
			navigationManager.LocationChanged -= NavigationManagerOnLocationChanged;
		}
		
		return;
		
		void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e) {
			if (Uri.TryCreate(e.Location, UriKind.Absolute, out var uri) && uri.AbsolutePath == newPath) {
				navigationTaskSource.SetResult();
			}
		}
	}
}
