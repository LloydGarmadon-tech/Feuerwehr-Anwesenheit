using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace FireDepartmentMvp.Data;

public static class DatabaseSchemaUpdater
{
    private static readonly string[] NewColumns =
    [
        "Description",
        "Object",
        "ObjectAddition",
        "KeywordAddition"
    ];

    public static async Task UpdateAsync(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            var existingColumns = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            await using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    "PRAGMA table_info(\"MissionDetails\")";

                await using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    existingColumns.Add(reader.GetString(1));
            }

            if (existingColumns.Count == 0)
                throw new InvalidOperationException(
                    "Die Tabelle MissionDetails wurde nicht gefunden.");

            foreach (var column in NewColumns)
            {
                if (existingColumns.Contains(column))
                    continue;

                await using var command = connection.CreateCommand();

                command.CommandText =
                    $"ALTER TABLE \"MissionDetails\" " +
                    $"ADD COLUMN \"{column}\" TEXT NULL";

                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
