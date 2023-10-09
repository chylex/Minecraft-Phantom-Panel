using System.Diagnostics.CodeAnalysis;
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
public class ApplicationDbContext : DbContext {
	public DbSet<UserEntity> Users { get; set; } = null!;
	public DbSet<RoleEntity> Roles { get; set; } = null!;
	public DbSet<PermissionEntity> Permissions { get; set; } = null!;
	
	public DbSet<UserRoleEntity> UserRoles { get; set; } = null!;
	public DbSet<UserPermissionEntity> UserPermissions { get; set; } = null!;
	public DbSet<RolePermissionEntity> RolePermissions { get; set; } = null!;
	
	public DbSet<AgentEntity> Agents { get; set; } = null!;
	public DbSet<InstanceEntity> Instances { get; set; } = null!;
	public DbSet<AuditLogEntity> AuditLog { get; set; } = null!;
	public DbSet<EventLogEntity> EventLog { get; set; } = null!;

	public AgentEntityUpsert AgentUpsert { get; }
	public InstanceEntityUpsert InstanceUpsert { get; }

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
		AgentUpsert = new AgentEntityUpsert(this);
		InstanceUpsert = new InstanceEntityUpsert(this);
	}

	protected override void OnModelCreating(ModelBuilder builder) {
		base.OnModelCreating(builder);

		builder.Entity<AuditLogEntity>(static b => {
			b.HasOne(static e => e.User).WithMany().HasForeignKey(static e => e.UserGuid).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
		});
		
		builder.Entity<UserEntity>(static b => {
			b.HasIndex(static e => e.Name).IsUnique();
		});
		
		builder.Entity<UserRoleEntity>(static b => {
			b.HasKey(static e => new { UserId = e.UserGuid, RoleId = e.RoleGuid });
			b.HasOne(static e => e.User).WithMany().HasForeignKey(static e => e.UserGuid).IsRequired().OnDelete(DeleteBehavior.Cascade);
			b.HasOne(static e => e.Role).WithMany().HasForeignKey(static e => e.RoleGuid).IsRequired().OnDelete(DeleteBehavior.Cascade);
		});
		
		builder.Entity<UserPermissionEntity>(static b => {
			b.HasKey(static e => new { UserId = e.UserGuid, e.PermissionId });
			b.HasOne<UserEntity>().WithMany().HasForeignKey(static e => e.UserGuid).IsRequired().OnDelete(DeleteBehavior.Cascade);
			b.HasOne<PermissionEntity>().WithMany().HasForeignKey(static e => e.PermissionId).IsRequired().OnDelete(DeleteBehavior.Cascade);
		});
		
		builder.Entity<RolePermissionEntity>(static b => {
			b.HasKey(static e => new { RoleId = e.RoleGuid, e.PermissionId });
			b.HasOne<RoleEntity>().WithMany().HasForeignKey(static e => e.RoleGuid).IsRequired().OnDelete(DeleteBehavior.Cascade);
			b.HasOne<PermissionEntity>().WithMany().HasForeignKey(static e => e.PermissionId).IsRequired().OnDelete(DeleteBehavior.Cascade);
		});
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder builder) {
		base.ConfigureConventions(builder);

		builder.Properties<AuditLogEventType>().HaveConversion<EnumToStringConverter<AuditLogEventType>>();
		builder.Properties<AuditLogSubjectType>().HaveConversion<EnumToStringConverter<AuditLogSubjectType>>();
		builder.Properties<EventLogEventType>().HaveConversion<EnumToStringConverter<EventLogEventType>>();
		builder.Properties<EventLogSubjectType>().HaveConversion<EnumToStringConverter<EventLogSubjectType>>();
		builder.Properties<MinecraftServerKind>().HaveConversion<EnumToStringConverter<MinecraftServerKind>>();
		builder.Properties<RamAllocationUnits>().HaveConversion<RamAllocationUnitsConverter>();
	}
}
