using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Users.SetUserPasswordErrors;

namespace Phantom.Common.Data.Web.Users {
	[MemoryPackable]
	[MemoryPackUnion(0, typeof(UserNotFound))]
	[MemoryPackUnion(1, typeof(PasswordIsInvalid))]
	[MemoryPackUnion(2, typeof(UnknownError))]
	public abstract partial record SetUserPasswordError {
		internal SetUserPasswordError() {}
	}
}

namespace Phantom.Common.Data.Web.Users.SetUserPasswordErrors {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record UserNotFound : SetUserPasswordError;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record PasswordIsInvalid([property: MemoryPackOrder(0)] ImmutableArray<PasswordRequirementViolation> Violations) : SetUserPasswordError;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record UnknownError : SetUserPasswordError;
}
