using MemoryPack;
using Phantom.Common.Data.Web.Users.UsernameRequirementViolations;

namespace Phantom.Common.Data.Web.Users {
	[MemoryPackable]
	[MemoryPackUnion(0, typeof(IsEmpty))]
	[MemoryPackUnion(1, typeof(TooLong))]
	public abstract partial record UsernameRequirementViolation {
		internal UsernameRequirementViolation() {}
	}
}

namespace Phantom.Common.Data.Web.Users.UsernameRequirementViolations {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record IsEmpty : UsernameRequirementViolation;
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record TooLong([property: MemoryPackOrder(0)] int MaxLength) : UsernameRequirementViolation;
}
