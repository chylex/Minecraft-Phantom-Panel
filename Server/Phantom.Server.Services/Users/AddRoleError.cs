namespace Phantom.Server.Services.Users; 

public enum AddRoleError : byte {
	NameIsEmpty,
	NameIsTooLong,
	NameAlreadyExists,
	UnknownError
}
