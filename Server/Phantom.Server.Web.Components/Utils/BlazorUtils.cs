using System.Globalization;

namespace Phantom.Server.Web.Components.Utils;

static class BlazorUtils {
	public static string? CombineClassNames(IReadOnlyDictionary<string, object>? additionalAttributes, string? classNames) {
		if (additionalAttributes is null || !additionalAttributes.TryGetValue("class", out var @class)) {
			return classNames;
		}

		var classAttributeValue = Convert.ToString(@class, CultureInfo.InvariantCulture);

		if (string.IsNullOrEmpty(classAttributeValue)) {
			return classNames;
		}

		if (string.IsNullOrEmpty(classNames)) {
			return classAttributeValue;
		}

		return $"{classAttributeValue} {classNames}";
	}

	public static bool CombineBooleansWithOr(IReadOnlyDictionary<string, object>? additionalAttributes, string attributeName, bool value) {
		return value || (additionalAttributes is not null && additionalAttributes.TryGetValue(attributeName, out var @bool) && @bool is bool and true);
	}
}
