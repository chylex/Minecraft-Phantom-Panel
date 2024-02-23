using MemoryPack;

namespace Phantom.Common.Data.Replies;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record InstanceActionResult<T>(
	[property: MemoryPackOrder(0)] InstanceActionGeneralResult GeneralResult,
	[property: MemoryPackOrder(1)] T? ConcreteResult
) {
	public bool Is(T? concreteResult) {
		return GeneralResult == InstanceActionGeneralResult.None && EqualityComparer<T>.Default.Equals(ConcreteResult, concreteResult);
	}

	public InstanceActionResult<T2> Map<T2>(Func<T, T2> mapper) {
		return new InstanceActionResult<T2>(GeneralResult, ConcreteResult is not null ? mapper(ConcreteResult) : default);
	}

	public string ToSentence(Func<T, string> concreteResultToSentence) {
		return GeneralResult switch {
			InstanceActionGeneralResult.None                 => concreteResultToSentence(ConcreteResult!),
			InstanceActionGeneralResult.AgentDoesNotExist    => "Agent does not exist.",
			InstanceActionGeneralResult.AgentShuttingDown    => "Agent is shutting down.",
			InstanceActionGeneralResult.AgentIsNotResponding => "Agent is not responding.",
			InstanceActionGeneralResult.InstanceDoesNotExist => "Instance does not exist.",
			_                                                => "Unknown result."
		};
	}
}

public static class InstanceActionResult {
	public static InstanceActionResult<T> General<T>(InstanceActionGeneralResult generalResult) {
		return new InstanceActionResult<T>(generalResult, default);
	}

	public static InstanceActionResult<T> Concrete<T>(T? concreteResult) {
		return new InstanceActionResult<T>(InstanceActionGeneralResult.None, concreteResult);
	}

	public static InstanceActionResult<T> DidNotReplyIfNull<T>(this InstanceActionResult<T>? result) {
		return result ?? General<T>(InstanceActionGeneralResult.AgentIsNotResponding);
	}
}
