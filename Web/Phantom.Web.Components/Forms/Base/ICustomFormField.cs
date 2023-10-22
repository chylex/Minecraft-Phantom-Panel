namespace Phantom.Web.Components.Forms.Base;

public interface ICustomFormField {
	bool TwoWayValueBinding { get; set; }
	void SetStringValue(string? value);
}
