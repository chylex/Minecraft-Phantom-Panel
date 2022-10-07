using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Phantom.Common.Data;
using Phantom.Server.Database.Converters;
using Phantom.Server.Database.Entities;
using Phantom.Server.Database.Factories;

namespace Phantom.Server.Database;

[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class ApplicationDbContext : DbContext {
	public DbSet<AgentEntity> Agents { get; set; } = null!;

	public AgentEntityUpsert AgentUpsert { get; }

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
		AgentUpsert = new AgentEntityUpsert(this);
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder builder) {
		base.ConfigureConventions(builder);

		builder.Properties<RamAllocationUnits>().HaveConversion<RamAllocationUnitsConverter>();
	}
}
