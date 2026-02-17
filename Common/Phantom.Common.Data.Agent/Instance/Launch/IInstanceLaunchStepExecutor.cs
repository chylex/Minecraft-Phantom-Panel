using System.Collections.Immutable;

namespace Phantom.Common.Data.Agent.Instance.Launch;

public interface IInstanceLaunchStepExecutor<TResult> {
	Task<TResult> DownloadFile(FileDownloadInfo downloadInfo, IInstancePath path);
	Task<TResult> EditPropertiesFile(InstancePath.Local path, string comment, ImmutableDictionary<string, string> newValues);
}
