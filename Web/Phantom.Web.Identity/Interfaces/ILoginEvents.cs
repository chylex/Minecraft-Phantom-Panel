using Phantom.Server.Database.Entities;

namespace Phantom.Server.Web.Identity.Interfaces; 

public interface ILoginEvents {
	void UserLoggedIn(UserEntity user);
	void UserLoggedOut(Guid userGuid);
}
