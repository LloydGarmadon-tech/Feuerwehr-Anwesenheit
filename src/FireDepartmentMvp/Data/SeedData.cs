using FireDepartmentMvp.Domain;
using Microsoft.EntityFrameworkCore;

namespace FireDepartmentMvp.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();

        await db.Database.EnsureCreatedAsync();

        if (await db.EventTypes.AnyAsync())
            return;

        var eventTypes = new[]
        {
            new EventType { Name = "Gruppendienst", Layout = AttendanceLayout.FiveGroups, SortOrder = 10 },
            new EventType { Name = "Sonderdienst", Layout = AttendanceLayout.FiveGroups, SortOrder = 20 },
            new EventType { Name = "Wehrdienst", Layout = AttendanceLayout.FiveGroups, SortOrder = 30 },
            new EventType { Name = "Zugdienst", Layout = AttendanceLayout.FiveGroups, SortOrder = 40 },
            new EventType { Name = "Übung", Layout = AttendanceLayout.SingleList, SortOrder = 50 },
            new EventType { Name = "Veranstaltung", Layout = AttendanceLayout.SingleList, SortOrder = 60 },
            new EventType { Name = "Maschinistendienst", Layout = AttendanceLayout.SingleList, SortOrder = 70 },
            new EventType { Name = "Führungsunterstützung", Layout = AttendanceLayout.SingleList, SortOrder = 80 },
            new EventType { Name = "Atemschutzdienst", Layout = AttendanceLayout.SingleList, SortOrder = 90 }
        };
        db.EventTypes.AddRange(eventTypes);

        var groups = Enumerable.Range(1, 5)
            .Select(i => new FirefighterGroup { Number = i, Name = $"Gruppe {i}" })
            .ToArray();
        db.FirefighterGroups.AddRange(groups);

        var qualifications = new[]
        {
            new Qualification { Name = "Atemschutzgeräteträger", ShortName = "AGT" },
            new Qualification { Name = "Maschinist", ShortName = "MA" },
            new Qualification { Name = "Gruppenführer", ShortName = "GF" },
            new Qualification { Name = "Zugführer", ShortName = "ZF" },
            new Qualification { Name = "Führungsunterstützung", ShortName = "FüU" }
        };
        db.Qualifications.AddRange(qualifications);

        var firstNames = new[] { "Lars", "Anna", "Martin", "Sven", "Nina", "Peter", "Thomas", "Julia", "Max", "Stefan", "Sarah", "Daniel", "Michael", "Laura", "Jan", "Tobias", "Lisa", "Christian", "Dennis", "Katharina", "Felix", "Miriam", "Patrick", "Sandra", "Björn" };
        var lastNames = new[] { "Schweet", "Muster", "Beispiel", "Schmidt", "Meyer", "Hoffmann", "Wagner", "Koch", "Bauer", "Richter", "Klein", "Wolf", "Neumann", "Schwarz", "Zimmermann", "Braun", "Krüger", "Hartmann", "Lange", "Werner", "Schmitz", "Krause", "Lehmann", "Köhler", "Maier" };

        var firefighters = new List<Firefighter>();
        for (var i = 0; i < 25; i++)
        {
            firefighters.Add(new Firefighter
            {
                FirstName = firstNames[i],
                LastName = lastNames[i],
                Group = groups[i % groups.Length]
            });
        }
        db.Firefighters.AddRange(firefighters);

        var hlf = new Vehicle { Name = "HLF 20", CallSign = "Florian 12/46-1" };
        hlf.Seats.AddRange(new[]
        {
            Seat(hlf, "Fahrer", 1),
            Seat(hlf, "Gruppenführer", 2),
            Seat(hlf, "Angriffstruppführer", 3),
            Seat(hlf, "Angriffstruppmann", 4),
            Seat(hlf, "Wassertruppführer", 5),
            Seat(hlf, "Wassertruppmann", 6),
            Seat(hlf, "Schlauchtruppführer", 7),
            Seat(hlf, "Schlauchtruppmann", 8),
            Seat(hlf, "Melder", 9)
        });

        var lf = new Vehicle { Name = "LF 10", CallSign = "Florian 12/45-1" };
        lf.Seats.AddRange(new[]
        {
            Seat(lf, "Fahrer", 1),
            Seat(lf, "Gruppenführer", 2),
            Seat(lf, "Angriffstruppführer", 3),
            Seat(lf, "Angriffstruppmann", 4),
            Seat(lf, "Wassertruppführer", 5),
            Seat(lf, "Wassertruppmann", 6)
        });

        var mtw = new Vehicle { Name = "MTW", CallSign = "Florian 12/19-1" };
        mtw.Seats.AddRange(new[]
        {
            Seat(mtw, "Fahrer", 1),
            Seat(mtw, "Beifahrer", 2),
            Seat(mtw, "Platz 1", 3),
            Seat(mtw, "Platz 2", 4),
            Seat(mtw, "Platz 3", 5),
            Seat(mtw, "Platz 4", 6),
            Seat(mtw, "Platz 5", 7),
            Seat(mtw, "Platz 6", 8)
        });

        db.Vehicles.AddRange(hlf, lf, mtw);
        await db.SaveChangesAsync();

        var agt = qualifications.Single(x => x.ShortName == "AGT");
        var ma = qualifications.Single(x => x.ShortName == "MA");
        var gf = qualifications.Single(x => x.ShortName == "GF");
        var fue = qualifications.Single(x => x.ShortName == "FüU");

        for (var i = 0; i < firefighters.Count; i++)
        {
            if (i % 2 == 0)
                db.FirefighterQualifications.Add(new FirefighterQualification { FirefighterId = firefighters[i].Id, QualificationId = agt.Id });
            if (i % 3 == 0)
                db.FirefighterQualifications.Add(new FirefighterQualification { FirefighterId = firefighters[i].Id, QualificationId = ma.Id });
            if (i % 5 == 0)
                db.FirefighterQualifications.Add(new FirefighterQualification { FirefighterId = firefighters[i].Id, QualificationId = gf.Id });
            if (i % 7 == 0)
                db.FirefighterQualifications.Add(new FirefighterQualification { FirefighterId = firefighters[i].Id, QualificationId = fue.Id });
        }

        await db.SaveChangesAsync();
    }

    private static VehicleSeat Seat(Vehicle vehicle, string name, int order) =>
        new() { Vehicle = vehicle, Name = name, SortOrder = order };
}
