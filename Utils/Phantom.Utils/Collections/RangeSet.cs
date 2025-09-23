using System.Collections;
using System.Numerics;

namespace Phantom.Utils.Collections;

public sealed class RangeSet<T> : IEnumerable<RangeSet<T>.Range> where T : IBinaryInteger<T> {
	private readonly List<Range> ranges = [];
	
	public bool Add(T value) {
		int index = 0;
		
		for (; index < ranges.Count; index++) {
			var range = ranges[index];
			if (range.Contains(value)) {
				return false;
			}
			
			if (range.ExtendIfAtEdge(value, out var extendedRange)) {
				ranges[index] = extendedRange;
				
				if (index < ranges.Count - 1) {
					var nextRange = ranges[index + 1];
					if (extendedRange.Max + T.One == nextRange.Min) {
						ranges[index] = new Range(extendedRange.Min, nextRange.Max);
						ranges.RemoveAt(index + 1);
					}
				}
				
				return true;
			}
			
			if (range.Max > value) {
				break;
			}
		}
		
		ranges.Insert(index, new Range(value, value));
		return true;
	}
	
	public List<Range>.Enumerator GetEnumerator() {
		return ranges.GetEnumerator();
	}
	
	IEnumerator<Range> IEnumerable<Range>.GetEnumerator() {
		return ranges.GetEnumerator();
	}
	
	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
	
	public readonly record struct Range(T Min, T Max) {
		internal bool ExtendIfAtEdge(T value, out Range newRange) {
			if (value == Min - T.One) {
				newRange = this with { Min = value };
				return true;
			}
			else if (value == Max + T.One) {
				newRange = this with { Max = value };
				return true;
			}
			else {
				newRange = default;
				return false;
			}
		}
		
		internal bool Contains(T value) {
			return value >= Min && value <= Max;
		}
	}
}
