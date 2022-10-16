using System.Text.RegularExpressions;

namespace Phantom.Server.Database.Enums;

public enum AuditEventType {
	AdministratorUserCreated,
	AdministratorUserModified,
	UserLoggedIn,
	UserLoggedOut,
	InstanceCreated,
	InstanceLaunched,
	InstanceStopped,
	InstanceCommandExecuted
}

public static partial class AuditEventCategoryExtensions {
	private static readonly Dictionary<AuditEventType, AuditSubjectType> SubjectTypes = new () {
		{ AuditEventType.AdministratorUserCreated,  AuditSubjectType.User },
		{ AuditEventType.AdministratorUserModified, AuditSubjectType.User },
		{ AuditEventType.UserLoggedIn,              AuditSubjectType.User },
		{ AuditEventType.UserLoggedOut,             AuditSubjectType.User },
		{ AuditEventType.InstanceLaunched,          AuditSubjectType.Instance },
		{ AuditEventType.InstanceStopped,           AuditSubjectType.Instance },
		{ AuditEventType.InstanceCommandExecuted,   AuditSubjectType.Instance }
	};

	internal static AuditSubjectType GetSubjectType(this AuditEventType type) {
		return SubjectTypes[type];
	}
	
	[GeneratedRegex(@"\B([A-Z])", RegexOptions.NonBacktracking)]
	private static partial Regex FindCapitalLettersRegex();

	public static string ToNiceString(this AuditEventType type) {
		return FindCapitalLettersRegex().Replace(type.ToString(), static match => " " + match.Groups[1].Value.ToLowerInvariant());
	}
}
