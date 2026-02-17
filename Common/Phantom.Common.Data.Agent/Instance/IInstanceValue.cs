using System.Collections.Immutable;
using System.Text;
using MemoryPack;

namespace Phantom.Common.Data.Agent.Instance;

[MemoryPackable]
[MemoryPackUnion(tag: 0, typeof(InstanceValues.Concatenation))]
[MemoryPackUnion(tag: 1, typeof(InstanceValues.Text))]
[MemoryPackUnion(tag: 2, typeof(InstanceValues.Path))]
public partial interface IInstanceValue {
	string? Resolve(IInstanceValueResolver resolver);
}

public static partial class InstanceValues {
	[MemoryPackable]
	public sealed partial record Concatenation(ImmutableArray<IInstanceValue> Values) : IInstanceValue {
		public string? Resolve(IInstanceValueResolver resolver) {
			var result = new StringBuilder();
			
			foreach (IInstanceValue value in Values) {
				if (value.Resolve(resolver) is {} resolved) {
					result.Append(resolved);
				}
				else {
					return null;
				}
			}
			
			return result.ToString();
		}
		
		public override string ToString() {
			return "Concatenation[" + string.Join(",", Values) + "]";
		}
	}
	
	[MemoryPackable]
	public sealed partial record Text(string Value) : IInstanceValue {
		public string Resolve(IInstanceValueResolver resolver) {
			return Value;
		}
		
		public override string ToString() {
			return "Text[" + Value + "]";
		}
	}
	
	[MemoryPackable]
	public sealed partial record Path(IInstancePath Value) : IInstanceValue {
		public string? Resolve(IInstanceValueResolver resolver) {
			return resolver.Path(Value);
		}
		
		public override string ToString() {
			return "Path[" + Value + "]";
		}
	}
}
