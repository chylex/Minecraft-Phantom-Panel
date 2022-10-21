using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Phantom.Common.Data;
using Phantom.Common.Data.Minecraft;
using Phantom.Server.Database.Converters;
using Phantom.Server.Database.Entities;
using Phantom.Server.Database.Enums;
using Phantom.Server.Database.Factories;

namespace Phantom.Server.Database;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class ApplicationDbContext : IdentityDbContext {
	public DbSet<PermissionEntity> Permissions { get; set; } = null!;
	public DbSet<UserPermissionEntity> UserPermissions { get; set; } = null!;
	public DbSet<RolePermissionEntity> RolePermissions { get; set; } = null!;
	
	public DbSet<AgentEntity> Agents { get; set; } = null!;
	public DbSet<InstanceEntity> Instances { get; set; } = null!;
	public DbSet<AuditEventEntity> AuditEvents { get; set; } = null!;

	public AgentEntityUpsert AgentUpsert { get; }
	public InstanceEntityUpsert InstanceUpsert { get; }

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
		AgentUpsert = new AgentEntityUpsert(this);
		InstanceUpsert = new InstanceEntityUpsert(this);
	}

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
		
		builder.Entity<UserPermissionEntity>(static b => {
			b.HasKey(static e => new { e.UserId, e.PermissionId });
			b.HasOne<IdentityUser>().WithMany().HasForeignKey(static e => e.UserId).IsRequired().OnDelete(DeleteBehavior.Cascade);
			b.HasOne<PermissionEntity>().WithMany().HasForeignKey(static e => e.PermissionId).IsRequired().OnDelete(DeleteBehavior.Cascade);
		});
		
		builder.Entity<RolePermissionEntity>(static b => {
			b.HasKey(static e => new { e.RoleId, e.PermissionId });
			b.HasOne<IdentityRole>().WithMany().HasForeignKey(static e => e.RoleId).IsRequired().OnDelete(DeleteBehavior.Cascade);
			b.HasOne<PermissionEntity>().WithMany().HasForeignKey(static e => e.PermissionId).IsRequired().OnDelete(DeleteBehavior.Cascade);
		});
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder builder) {
		base.ConfigureConventions(builder);

		builder.Properties<AuditEventType>().HaveConversion<EnumToStringConverter<AuditEventType>>();
		builder.Properties<AuditSubjectType>().HaveConversion<EnumToStringConverter<AuditSubjectType>>();
		builder.Properties<MinecraftServerKind>().HaveConversion<EnumToStringConverter<MinecraftServerKind>>();
		builder.Properties<RamAllocationUnits>().HaveConversion<RamAllocationUnitsConverter>();
	}
}
