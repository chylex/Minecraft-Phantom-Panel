namespace Phantom.Common.Data.Web.Users;

public enum AddRoleError : byte {
	NameIsEmpty,
	NameIsTooLong,
	NameAlreadyExists,
	UnknownError
}
