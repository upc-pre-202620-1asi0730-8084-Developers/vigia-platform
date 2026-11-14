namespace VigiaPlatform.API.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using System.Data;
using Microsoft.EntityFrameworkCore;

public static class DatabaseMigrationRunner
{
    public static async Task MigrateAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Database.CanConnectAsync(cancellationToken))
        {
            var connection = db.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;
            long existingTables;
            try
            {
                if (openedHere) await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND LOWER(table_name) <> '__efmigrationshistory'";
                existingTables = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            }
            finally
            {
                if (openedHere) await connection.CloseAsync();
            }
            var initialMigration = db.Database.GetMigrations().FirstOrDefault();
            var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
            if (existingTables > 0 && (initialMigration is null || !applied.Contains(initialMigration)))
                throw new InvalidOperationException("Existing tables do not belong to this migration chain. Select the migrated local database or prepare a reviewed transition from EnsureCreated before applying migrations. No schema changes were made.");
        }
        await db.Database.MigrateAsync(cancellationToken);
    }
}
