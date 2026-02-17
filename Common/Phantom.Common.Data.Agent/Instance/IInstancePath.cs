using System.Collections.Immutable;
using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(InstancePath.Global))]
[MemoryPackUnion(tag: 1, typeof(InstancePath.Local))]
[MemoryPackUnion(tag: 2, typeof(InstancePath.Runtime))]
public partial interface IInstancePath {
	string? Resolve(IInstancePathResolver resolver);
}

public static partial class InstancePath {
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Global(
		[property: MemoryPackOrder(0)] ImmutableArray<string> Segments
	) : IInstancePath {
		public string? Resolve(IInstancePathResolver resolver) {
			return resolver.Global(Segments);
		}
		
		public override string ToString() {
			return "Global[" + string.Join(separator: '/', Segments) + "]";
		}
	}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Local(
		[property: MemoryPackOrder(0)] ImmutableArray<string> Segments
	) : IInstancePath {
		public string? Resolve(IInstancePathResolver resolver) {
			return resolver.Local(Segments);
		}
		
		public override string ToString() {
			return "Local[" + string.Join(separator: '/', Segments) + "]";
		}
	}
	
	[MemoryPackable(GenerateType.VersionTolerant)]
	public sealed partial record Runtime(
		[property: MemoryPackOrder(0)] Guid Guid
	) : IInstancePath {
		public string? Resolve(IInstancePathResolver resolver) {
			return resolver.Runtime(Guid);
		}
		
		public override string ToString() {
			return "Runtime[" + Guid + "]";
		}
	}
}
