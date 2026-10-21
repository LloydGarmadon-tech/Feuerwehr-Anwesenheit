using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace FireDepartmentMvp.Data;

public static class DatabaseSchemaUpdater
{
    public static async Task UpdateAsync(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await AddMissingColumnsAsync(
                connection,
                "MissionDetails",
                [
                    ("Description", "TEXT NULL"),
                    ("Object", "TEXT NULL"),
                    ("ObjectAddition", "TEXT NULL"),
                    ("KeywordAddition", "TEXT NULL")
                ]);

            await AddMissingColumnsAsync(
                connection,
                "Vehicles",
                [
                    ("Fe2Identifier", "TEXT NOT NULL DEFAULT ''"),
                    ("UseSeatAssignment", "INTEGER NOT NULL DEFAULT 1")
                ]);

            await CreateActivityVehiclesTableAsync(connection);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static async Task CreateActivityVehiclesTableAsync(
        DbConnection connection)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "ActivityVehicles" (
                "ActivityId" TEXT NOT NULL,
                "VehicleId" TEXT NOT NULL,

                CONSTRAINT "PK_ActivityVehicles"
                    PRIMARY KEY ("ActivityId", "VehicleId"),

                CONSTRAINT "FK_ActivityVehicles_Activities"
                    FOREIGN KEY ("ActivityId")
                    REFERENCES "Activities" ("Id")
                    ON DELETE CASCADE,

                CONSTRAINT "FK_ActivityVehicles_Vehicles"
                    FOREIGN KEY ("VehicleId")
                    REFERENCES "Vehicles" ("Id")
                    ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS "IX_ActivityVehicles_VehicleId"
                ON "ActivityVehicles" ("VehicleId");
            """;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task AddMissingColumnsAsync(
        DbConnection connection,
        string table,
        (string Name, string Definition)[] columns)
    {
        var existingColumns = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info(\"{table}\")";

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                existingColumns.Add(reader.GetString(1));
        }

        if (existingColumns.Count == 0)
            throw new InvalidOperationException(
                $"Die Tabelle {table} wurde nicht gefunden.");

        foreach (var (name, definition) in columns)
        {
            if (existingColumns.Contains(name))
                continue;

            await using var command = connection.CreateCommand();

            command.CommandText =
                $"ALTER TABLE \"{table}\" ADD COLUMN \"{name}\" {definition}";

            await command.ExecuteNonQueryAsync();
        }
    }
}
