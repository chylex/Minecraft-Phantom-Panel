namespace Phantom.Agent;

readonly struct AuthTokenSource {
	public string? Token { get; init; }
	public string? TokenFilePath { get; init; }

	public string Read() {
		if (Token != null) {
			return Token;
		}
		else if (TokenFilePath != null) {
			return File.ReadAllText(TokenFilePath).TrimEnd();
		}
		else {
			throw new InvalidOperationException();
		}
	}
}
