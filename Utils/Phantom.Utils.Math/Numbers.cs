using System.Numerics;

namespace Phantom.Utils.Math; 

public static class Numbers {
	public static T Min<T>(T a, T b, T c) where T : INumber<T> {
		return T.Min(a, T.Min(b, c));
	}
}
