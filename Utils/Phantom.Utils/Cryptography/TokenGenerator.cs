using System.Security.Cryptography;
using System.Text;

namespace Phantom.Utils.Cryptography;

public static class TokenGenerator {
	private const string AllowedCharacters = "25679BCDFGHJKMNPQRSTWXYZ";
	
	private static readonly HashSet<char> AllowedCharacterSet = new (AllowedCharacters);
	private static readonly Base24 Base24 = new (AllowedCharacters);
	
	public static string Create(int length) {
		char[] result = new char[length];
		
		for (int i = 0; i < length; i++) {
			result[i] = AllowedCharacters[RandomNumberGenerator.GetInt32(AllowedCharacters.Length)];
		}
		
		return new string(result);
	}
	
	public static byte[] GetBytesOrThrow(string token) {
		if (token.Length == 0) {
			throw new ArgumentOutOfRangeException(nameof(token), "Invalid token (empty).");
		}
		
		foreach (char c in token) {
			if (!AllowedCharacterSet.Contains(c)) {
				throw new ArgumentOutOfRangeException(nameof(token), "Invalid token: " + token);
			}
		}
		
		return Encoding.ASCII.GetBytes(token);
	}
	
	public static string EncodeBytes(byte[] bytes) {
		return Base24.Encode(bytes);
	}
	
	public static byte[] DecodeBytes(string token) {
		return Base24.Decode(token);
	}
}
