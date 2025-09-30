using MemoryPack;

namespace Phantom.Common.Data.Web.Users;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(IsEmpty))]
[MemoryPackUnion(tag: 1, typeof(TooLong))]
public abstract partial record UsernameRequirementViolation {
	private UsernameRequirementViolation() {}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record IsEmpty : UsernameRequirementViolation;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record TooLong([property: MemoryPackOrder(0)] int MaxLength) : UsernameRequirementViolation;
}
