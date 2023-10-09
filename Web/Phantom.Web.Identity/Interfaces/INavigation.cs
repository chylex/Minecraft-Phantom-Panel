using System.Diagnostics.CodeAnalysis;

namespace Phantom.Web.Identity.Interfaces;

public interface INavigation {
	string BasePath { get; }
	bool GetQueryParameter(string key, [MaybeNullWhen(false)] out string value);
	void NavigateTo(string url, bool forceLoad = false);
}
