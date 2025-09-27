using System.ComponentModel.DataAnnotations;

namespace Phantom.Web.Components.Utils;

public abstract class FormValidationAttribute<TModel, TValue> : ValidationAttribute {
	public sealed override bool IsValid(object? value) {
		return base.IsValid(value);
	}
	
	protected sealed override ValidationResult? IsValid(object? value, ValidationContext validationContext) {
		var model = (TModel) validationContext.ObjectInstance;
		return value is TValue typedValue && IsValid(model, typedValue) ? ValidationResult.Success : new ValidationResult(errorMessage: null, [FieldName]);
	}
	
	protected abstract string FieldName { get; }
	protected abstract bool IsValid(TModel model, TValue value);
}
