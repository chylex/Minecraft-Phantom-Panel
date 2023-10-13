using MemoryPack;
using Phantom.Common.Data.Web.Users.CreateOrUpdateAdministratorUserResults;

namespace Phantom.Common.Data.Web.Users {
	[MemoryPackable]
	[MemoryPackUnion(0, typeof(Success))]
	[MemoryPackUnion(1, typeof(CreationFailed))]
	[MemoryPackUnion(2, typeof(UpdatingFailed))]
	[MemoryPackUnion(3, typeof(AddingToRoleFailed))]
	[MemoryPackUnion(4, typeof(UnknownError))]
	public abstract partial record CreateOrUpdateAdministratorUserResult {
		internal CreateOrUpdateAdministratorUserResult() {}
	}
}

namespace Phantom.Common.Data.Web.Users.CreateOrUpdateAdministratorUserResults {
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
