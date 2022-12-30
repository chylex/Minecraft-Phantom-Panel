namespace Phantom.Server.Web.Identity.Interfaces; 

public interface ILoginEvents {
	void UserLoggedIn(string userId);
	void UserLoggedOut(string userId);
}
