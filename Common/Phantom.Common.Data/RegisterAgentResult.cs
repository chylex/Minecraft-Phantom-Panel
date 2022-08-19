namespace Phantom.Common.Data; 

public enum RegisterAgentResult : byte {
	Success,
	DuplicateConnection,
	OldConnectionNotClosed,
	InvalidToken
}
