using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Phantom.Server.Web.Database;

public class ApplicationDbContext : IdentityDbContext {
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
}
