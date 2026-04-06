namespace Phantom.Common.Data.Agent.Instance.Stop;

public interface IInstanceStopStepExecutor<TResult> {
	Task<TResult> Wait(TimeSpan duration);
	Task<TResult> SendToStandardInput(IInstanceValue line);
}
