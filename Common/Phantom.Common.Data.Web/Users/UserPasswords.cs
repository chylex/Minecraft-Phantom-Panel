namespace Phantom.Common.Data.Web.Users;

public static class UserPasswords {
	public static string Hash(string password) {
		return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
	}
	
	public static bool Verify(string password, string hash) {
		// TODO rehash
		return BCrypt.Net.BCrypt.Verify(password, hash);
	}
}
