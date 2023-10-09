namespace Phantom.Controller.Services.Users.Roles;

public enum AddRoleError : byte {
	NameIsEmpty,
	NameIsTooLong,
	NameAlreadyExists,
	UnknownError
}
