using System.ComponentModel.DataAnnotations;

namespace Phantom.Server.Web.Components.Utils; 

public abstract class FormValidationAttribute : ValidationAttribute {
	public sealed override bool IsValid(object? value) {
		return base.IsValid(value);
	}

	protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
		return IsValid(validationContext.ObjectInstance, value) ? ValidationResult.Success : new ValidationResult(null, new [] { FieldName });
	}

	protected abstract string FieldName { get; }
	protected abstract bool IsValid(object model, object? value);
}
