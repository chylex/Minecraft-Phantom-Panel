namespace Phantom.Utils.Collections;

public static class Comparables {
	public static T Min<T>(T first, T second) where T : IComparable<T> {
		return first.CompareTo(second) < 0 ? first : second;
	}
	
	public static T Max<T>(T first, T second) where T : IComparable<T> {
		return first.CompareTo(second) < 0 ? second : first;
	}
}
