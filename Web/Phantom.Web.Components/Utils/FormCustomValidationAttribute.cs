using System.ComponentModel.DataAnnotations;

namespace Phantom.Web.Components.Utils;

public abstract class FormCustomValidationAttribute<TModel, TValue> : ValidationAttribute {
	public sealed override bool IsValid(object? value) {
		return base.IsValid(value);
	}

	protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
		if (value is not TValue typedValue) {
			return new ValidationResult(null, new [] { FieldName });
		}

		var model = (TModel) validationContext.ObjectInstance;
		var result = Validate(model, typedValue);
		return result == ValidationResult.Success ? result : new ValidationResult(result?.ErrorMessage, new [] { FieldName });
	}

	protected abstract string FieldName { get; }
	protected abstract ValidationResult? Validate(TModel model, TValue value);
}
