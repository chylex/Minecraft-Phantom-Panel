using System.ComponentModel.DataAnnotations;

namespace Phantom.Server.Web.Components.Utils;

public abstract class FormCustomValidationAttribute : ValidationAttribute {
	public sealed override bool IsValid(object? value) {
		return base.IsValid(value);
	}

	protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
		var result = Validate(validationContext.ObjectInstance, value);
		return result == ValidationResult.Success ? result : new ValidationResult(result?.ErrorMessage, new [] { FieldName });
	}

	protected abstract string FieldName { get; }
	protected abstract ValidationResult? Validate(object model, object? value);
}
