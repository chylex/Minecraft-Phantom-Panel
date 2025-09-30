using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(NameIsInvalid))]
[MemoryPackUnion(tag: 1, typeof(PasswordIsInvalid))]
[MemoryPackUnion(tag: 2, typeof(NameAlreadyExists))]
[MemoryPackUnion(tag: 3, typeof(UnknownError))]
public abstract partial record AddUserError {
	private AddUserError() {}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record NameIsInvalid([property: MemoryPackOrder(0)] UsernameRequirementViolation Violation) : AddUserError;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record PasswordIsInvalid([property: MemoryPackOrder(0)] ImmutableArray<PasswordRequirementViolation> Violations) : AddUserError;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record NameAlreadyExists : AddUserError;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record UnknownError : AddUserError;
}
