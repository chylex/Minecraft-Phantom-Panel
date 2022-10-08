using System.Diagnostics.CodeAnalysis;
using System.Web;
using Microsoft.AspNetCore.Components;

namespace Phantom.Server.Web.Components.Utils; 

public static class Query {
	public static bool GetParameter(NavigationManager navigationManager, string key, [MaybeNullWhen(false)] out string value) {
		var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
		var query = HttpUtility.ParseQueryString(uri.Query);
		
		value = query.Get(key);
		return value != null;
	}
}
