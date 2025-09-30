using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(Success))]
[MemoryPackUnion(tag: 1, typeof(CreationFailed))]
[MemoryPackUnion(tag: 2, typeof(UpdatingFailed))]
[MemoryPackUnion(tag: 3, typeof(AddingToRoleFailed))]
[MemoryPackUnion(tag: 4, typeof(UnknownError))]
public abstract partial record CreateOrUpdateAdministratorUserResult {
	private CreateOrUpdateAdministratorUserResult() {}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Success([property: MemoryPackOrder(0)] UserInfo User) : CreateOrUpdateAdministratorUserResult;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record CreationFailed([property: MemoryPackOrder(0)] AddUserError Error) : CreateOrUpdateAdministratorUserResult;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record UpdatingFailed([property: MemoryPackOrder(0)] SetUserPasswordError Error) : CreateOrUpdateAdministratorUserResult;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record AddingToRoleFailed : CreateOrUpdateAdministratorUserResult;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record UnknownError : CreateOrUpdateAdministratorUserResult;
}
