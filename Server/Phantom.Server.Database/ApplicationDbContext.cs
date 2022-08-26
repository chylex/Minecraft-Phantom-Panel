using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Phantom.Server.Database.Entities;

namespace Phantom.Server.Database;

public class ApplicationDbContext : IdentityDbContext {
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

	protected override void OnModelCreating(ModelBuilder builder) {
		base.OnModelCreating(builder);

		const string IdentitySchema = "identity";
		
		builder.Entity<IdentityRole>().ToTable("Roles", schema: IdentitySchema);
		builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", schema: IdentitySchema);
		
		builder.Entity<IdentityUser>().ToTable("Users", schema: IdentitySchema);
		builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", schema: IdentitySchema);
		builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", schema: IdentitySchema);
		builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", schema: IdentitySchema);
		builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", schema: IdentitySchema);
	}
	
	public DbSet<AgentEntity> Agents { get; set; } = null!;
}
