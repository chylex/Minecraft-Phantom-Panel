using MemoryPack;
using Phantom.Common.Data.Web.Users.PasswordRequirementViolations;

namespace Phantom.Common.Data.Web.Users {
	[MemoryPackable]
	[MemoryPackUnion(0, typeof(TooShort))]
	[MemoryPackUnion(1, typeof(MustContainLowercaseLetter))]
	[MemoryPackUnion(2, typeof(MustContainUppercaseLetter))]
	[MemoryPackUnion(3, typeof(MustContainDigit))]
	public abstract partial record PasswordRequirementViolation {
		internal PasswordRequirementViolation() {}
	}
}

namespace Phantom.Common.Data.Web.Users.PasswordRequirementViolations {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record TooShort([property: MemoryPackOrder(0)] int MinimumLength) : PasswordRequirementViolation;

	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record MustContainLowercaseLetter : PasswordRequirementViolation;

	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record MustContainUppercaseLetter : PasswordRequirementViolation;

	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record MustContainDigit : PasswordRequirementViolation;
}
