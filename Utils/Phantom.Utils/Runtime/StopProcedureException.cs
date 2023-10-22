namespace Phantom.Utils.Runtime;

/// <summary>
/// Custom exception used to signal a procedure should stop. This exception should not be logged or propagated.
/// </summary>
public sealed class StopProcedureException : Exception {
	public static StopProcedureException Instance { get; } = new ();

	private StopProcedureException() {}
}
