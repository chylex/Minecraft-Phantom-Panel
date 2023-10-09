using System.Text.RegularExpressions;

namespace Phantom.Web.Utils;

static partial class EnumNameConverter {
	[GeneratedRegex(@"\B([A-Z])", RegexOptions.NonBacktracking)]
	private static partial Regex FindCapitalLettersRegex();

	public static string ToNiceString<T>(this T type) where T : Enum {
		return FindCapitalLettersRegex().Replace(type.ToString(), static match => " " + match.Groups[1].Value.ToLowerInvariant());
	}
}
