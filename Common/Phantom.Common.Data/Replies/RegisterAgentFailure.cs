namespace Phantom.Common.Data.Replies; 

public enum RegisterAgentFailure : byte {
	DuplicateConnection,
	OldConnectionNotClosed,
	InvalidToken
}
