namespace Phantom.Common.Data.Web.Agent;

public enum CreateOrUpdateAgentResult : byte {
	UnknownError,
	Success,
	AgentNameMustNotBeEmpty,
}

public static class CreateOrUpdateAgentResultExtensions {
	public static string ToSentence(this CreateOrUpdateAgentResult reason) {
		return reason switch {
			CreateOrUpdateAgentResult.Success                 => "Success.",
			CreateOrUpdateAgentResult.AgentNameMustNotBeEmpty => "Agent name must not be empty.",
			_                                                 => "Unknown error.",
		};
	}
}
