using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Users.AddUserErrors;

namespace Phantom.Common.Data.Web.Users {
	[MemoryPackable]
	[MemoryPackUnion(0, typeof(NameIsInvalid))]
	[MemoryPackUnion(1, typeof(PasswordIsInvalid))]
	[MemoryPackUnion(2, typeof(NameAlreadyExists))]
	[MemoryPackUnion(3, typeof(UnknownError))]
	public abstract partial record AddUserError {
		internal AddUserError() {}
	}
}

namespace Phantom.Common.Data.Web.Users.AddUserErrors {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record NameIsInvalid([property: MemoryPackOrder(0)] UsernameRequirementViolation Violation) : AddUserError;

	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record PasswordIsInvalid([property: MemoryPackOrder(0)] ImmutableArray<PasswordRequirementViolation> Violations) : AddUserError;

	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record NameAlreadyExists : AddUserError;

	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record UnknownError : AddUserError;
}
