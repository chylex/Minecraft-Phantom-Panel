using System.Security.Cryptography;

namespace Phantom.Utils.Cryptography; 

public static class TokenGenerator {
	private const string AllowedCharacters = "25679bcdfghjkmnpqrstwxyz";
	
	public static string Create(int length) {
		char[] result = new char[length];

		for (int i = 0; i < length; i++) {
			result[i] = AllowedCharacters[RandomNumberGenerator.GetInt32(AllowedCharacters.Length)];
		}
		
		return new string(result);
	}
}
