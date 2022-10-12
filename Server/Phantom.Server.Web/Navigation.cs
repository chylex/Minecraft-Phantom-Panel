using System.Diagnostics.CodeAnalysis;
using System.Web;
using Microsoft.AspNetCore.Components;

namespace Phantom.Server.Web; 

sealed class Navigation {
	public static Func<IServiceProvider, Navigation> Create(string basePath) {
		return provider => new Navigation(basePath, provider.GetRequiredService<NavigationManager>());
	}

	public string BasePath { get; }
	public string CurrentRelativePath => navigationManager.ToBaseRelativePath(navigationManager.Uri);
	
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
	
	public void NavigateTo(string url, bool forceLoad = false) {
		navigationManager.NavigateTo(BasePath + url, forceLoad);
	}
}
