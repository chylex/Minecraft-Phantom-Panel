using MemoryPack;
using Phantom.Common.Data.Replies;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(OfUserActionFailure))]
[MemoryPackUnion(tag: 1, typeof(OfInstanceActionFailure))]
public abstract partial record UserInstanceActionFailure {
	internal UserInstanceActionFailure() {}
	
	public static implicit operator UserInstanceActionFailure(UserActionFailure failure) {
		return new OfUserActionFailure(failure);
	}
	
	public static implicit operator UserInstanceActionFailure(InstanceActionFailure failure) {
		return new OfInstanceActionFailure(failure);
	}
}

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record OfUserActionFailure([property: MemoryPackOrder(0)] UserActionFailure Failure) : UserInstanceActionFailure;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record OfInstanceActionFailure([property: MemoryPackOrder(0)] InstanceActionFailure Failure) : UserInstanceActionFailure;
