using Pricely.Common.Infrastructure.Inbox;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Modules.Users.Application.Abstractions.Data;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;

namespace Pricely.Modules.Users.Infrastructure.Database;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Users);

        // Case-insensitive text columns (emails, names, codes): PostgreSQL text comparisons are case-sensitive (ADR-012).
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
    }
}
