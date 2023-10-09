using Phantom.Controller.Database.Entities;

namespace Phantom.Web.Identity.Interfaces; 

public interface ILoginEvents {
	void UserLoggedIn(UserEntity user);
	void UserLoggedOut(Guid userGuid);
}
