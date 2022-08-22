namespace Phantom.Common.Data.Replies; 

public enum RegisterAgentResult : byte {
	Success,
	DuplicateConnection,
	OldConnectionNotClosed,
	InvalidToken
}
