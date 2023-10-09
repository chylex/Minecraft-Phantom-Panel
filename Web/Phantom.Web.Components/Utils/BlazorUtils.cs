using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Phantom.Web.Components.Utils;

static class BlazorUtils {
	[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
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

	[SuppressMessage("ReSharper", "MergeAndPattern")]
	public static bool CombineBooleansWithOr(IReadOnlyDictionary<string, object>? additionalAttributes, string attributeName, bool value) {
		return value || (additionalAttributes is not null && additionalAttributes.TryGetValue(attributeName, out var @bool) && @bool is bool and true);
	}

	public static void RequireEitherParameterIsSet<T1, T2>(
		T1 parameterValue1,
		T2 parameterValue2,
		[CallerArgumentExpression(nameof(parameterValue1))]
		string parameterName1 = "",
		[CallerArgumentExpression(nameof(parameterValue2))]
		string parameterName2 = ""
	) {
		if (parameterValue1 is null && parameterValue2 is null) {
			throw new InvalidOperationException($"Either {parameterName1} or {parameterName2} must be set.");
		}
		else if (parameterValue1 is not null && parameterValue2 is not null) {
			throw new InvalidOperationException($"Either {parameterName1} or {parameterName2} must be set, but not both.");
		}
	}
}
