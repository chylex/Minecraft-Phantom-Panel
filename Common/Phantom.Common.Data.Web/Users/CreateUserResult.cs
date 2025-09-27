using MemoryPack;
using Phantom.Common.Data.Web.Users.CreateUserResults;

namespace Phantom.Common.Data.Web.Users {
	[MemoryPackable]
	[MemoryPackUnion(tag: 0, typeof(Success))]
	[MemoryPackUnion(tag: 1, typeof(CreationFailed))]
	[MemoryPackUnion(tag: 2, typeof(UnknownError))]
	public abstract partial record CreateUserResult {
		internal CreateUserResult() {}
	}
}

namespace Phantom.Common.Data.Web.Users.CreateUserResults {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Success([property: MemoryPackOrder(0)] UserInfo User) : CreateUserResult;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record CreationFailed([property: MemoryPackOrder(0)] AddUserError Error) : CreateUserResult;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record UnknownError : CreateUserResult;
}
