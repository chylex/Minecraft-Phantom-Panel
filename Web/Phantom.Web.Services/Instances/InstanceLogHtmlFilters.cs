using System.Net;
using System.Text.RegularExpressions;

namespace Phantom.Web.Services.Instances;

static partial class InstanceLogHtmlFilters {
	/// <summary>
	/// Matches IPv4 addresses.
	/// </summary>
	[GeneratedRegex(@"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b", RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
	private static partial Regex Ipv4();

	/// <summary>
	/// Matches full IPv6 addresses in square brackets. Does not match compressed IPv6 addresses since Java's <c>toString()</c> currently expands them.
	/// </summary>
	[GeneratedRegex(@"\[([0-9a-fA-F]{1,4}:){7}([0-9a-fA-F]{1,4})\]", RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture)]
	private static partial Regex Ipv6();
	
	public static string Process(string line) {
		line = WebUtility.HtmlEncode(line);
		line = Ipv4().Replace(line, "<span class='text-redacted'>x.x.x.x</span>");
		line = Ipv6().Replace(line, "<span class='text-redacted'>x::x</span>");
		return line;
	}
}
