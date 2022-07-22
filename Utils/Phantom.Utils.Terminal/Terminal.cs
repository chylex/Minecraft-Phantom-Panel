namespace Phantom.Utils.Terminal; 

public static class Terminal {
	public static void PrintDelimiter() {
		Console.WriteLine(new string('-', Math.Min(50, Console.BufferWidth)));
	}

	public static void PrintLine(string line) {
		Console.WriteLine(line);
	}
}
