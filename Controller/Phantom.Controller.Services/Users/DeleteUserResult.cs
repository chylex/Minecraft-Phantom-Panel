namespace Phantom.Server.Services.Users; 

public enum DeleteUserResult : byte {
	Deleted,
	NotFound,
	Failed
}
