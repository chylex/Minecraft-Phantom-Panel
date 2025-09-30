using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(User))]
[MemoryPackUnion(tag: 1, typeof(Instance))]
public abstract partial record UserInstanceActionFailure {
	private UserInstanceActionFailure() {}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record User([property: MemoryPackOrder(0)] UserActionFailure Failure) : UserInstanceActionFailure;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Instance([property: MemoryPackOrder(0)] InstanceActionFailure Failure) : UserInstanceActionFailure;
	
	public static implicit operator UserInstanceActionFailure(UserActionFailure failure) {
		return new User(failure);
	}
	
	public static implicit operator UserInstanceActionFailure(InstanceActionFailure failure) {
		return new Instance(failure);
	}
}
