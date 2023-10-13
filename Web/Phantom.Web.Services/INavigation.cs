using System.Diagnostics.CodeAnalysis;

namespace Phantom.Web.Services;

public interface INavigation {
	string BasePath { get; }
	bool GetQueryParameter(string key, [MaybeNullWhen(false)] out string value);
	Task NavigateTo(string url, bool forceLoad = false);
}
