namespace Phantom.Controller.Services.Users;

public enum AddRoleError : byte {
	NameIsEmpty,
	NameIsTooLong,
	NameAlreadyExists,
	UnknownError
}
