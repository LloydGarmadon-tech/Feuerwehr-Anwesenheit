using FireDepartmentMvp.Data;
using FireDepartmentMvp.Domain;
using Microsoft.EntityFrameworkCore;

namespace FireDepartmentMvp.Services;

public class AdminService(IDbContextFactory<AppDbContext> factory, AppState state)
{
    public async Task<List<FirefighterGroup>> GetGroupsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.FirefighterGroups.AsNoTracking().OrderBy(x => x.Number).ToListAsync();
    }

    public async Task<List<Firefighter>> GetFirefightersAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Firefighters.AsNoTracking()
            .Include(x => x.Group)
            .Include(x => x.Qualifications).ThenInclude(x => x.Qualification)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .ToListAsync();
    }

    public async Task SaveFirefighterAsync(Guid? id, string firstName, string lastName, Guid groupId, bool isActive, IEnumerable<Guid> qualificationIds)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new InvalidOperationException("Vor- und Nachname sind erforderlich.");

        await using var db = await factory.CreateDbContextAsync();
        if (!await db.FirefighterGroups.AnyAsync(x => x.Id == groupId && x.IsActive))
            throw new InvalidOperationException("Bitte eine aktive Gruppe auswählen.");

        Firefighter person;
        if (id.HasValue)
        {
            person = await db.Firefighters.Include(x => x.Qualifications).SingleAsync(x => x.Id == id.Value);
        }
        else
        {
            person = new Firefighter();
            db.Firefighters.Add(person);
        }

        person.FirstName = firstName.Trim();
        person.LastName = lastName.Trim();
        person.GroupId = groupId;
        person.IsActive = isActive;

        var wanted = qualificationIds.ToHashSet();
        person.Qualifications.RemoveAll(x => !wanted.Contains(x.QualificationId));
        foreach (var qid in wanted.Where(qid => person.Qualifications.All(x => x.QualificationId != qid)))
            person.Qualifications.Add(new FirefighterQualification { FirefighterId = person.Id, QualificationId = qid });

        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    public async Task<List<Vehicle>> GetVehiclesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Vehicles.AsNoTracking().Include(x => x.Seats)
            .OrderBy(x => x.Name).ToListAsync();
    }


public async Task<Vehicle> SaveVehicleAsync(
    Guid? id,
    string name,
    string callSign,
    bool isActive,
    string fe2Identifier,
    bool useSeatAssignment)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new InvalidOperationException(
            "Der Fahrzeugname ist erforderlich.");

    name = name.Trim();
    callSign = callSign?.Trim() ?? string.Empty;
    fe2Identifier = fe2Identifier?.Trim() ?? string.Empty;

    await using var db = await factory.CreateDbContextAsync();

    if (!string.IsNullOrEmpty(fe2Identifier))
    {
        var duplicate = await db.Vehicles.AnyAsync(x =>
            x.Fe2Identifier == fe2Identifier &&
            (!id.HasValue || x.Id != id.Value));

        if (duplicate)
            throw new InvalidOperationException(
                "Diese FE2-Kennung ist bereits einem anderen Fahrzeug zugeordnet.");
    }

    Vehicle vehicle;

    if (id.HasValue)
    {
        vehicle = await db.Vehicles.SingleAsync(x => x.Id == id.Value);
    }
    else
    {
        vehicle = new Vehicle();
        db.Vehicles.Add(vehicle);
    }

    vehicle.Name = name;
    vehicle.CallSign = callSign;
    vehicle.Fe2Identifier = fe2Identifier;
    vehicle.UseSeatAssignment = useSeatAssignment;
    vehicle.IsActive = isActive;

    await db.SaveChangesAsync();
    state.NotifyChanged();

    return vehicle;
}


    public async Task AddSeatAsync(Guid vehicleId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Der Sitzplatzname ist erforderlich.");

        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Vehicles.AnyAsync(x => x.Id == vehicleId))
            throw new InvalidOperationException("Fahrzeug nicht gefunden.");

        var maxOrder = await db.VehicleSeats.Where(x => x.VehicleId == vehicleId)
            .Select(x => (int?)x.SortOrder).MaxAsync() ?? 0;
        db.VehicleSeats.Add(new VehicleSeat { VehicleId = vehicleId, Name = name.Trim(), SortOrder = maxOrder + 1, IsActive = true });
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    public async Task ToggleSeatAsync(Guid seatId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var seat = await db.VehicleSeats.SingleAsync(x => x.Id == seatId);
        seat.IsActive = !seat.IsActive;
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    public async Task<List<Qualification>> GetQualificationsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Qualifications.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    }

    public async Task SaveQualificationAsync(Guid? id, string name, string shortName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(shortName))
            throw new InvalidOperationException("Name und Kürzel sind erforderlich.");

        await using var db = await factory.CreateDbContextAsync();
        Qualification item;
        if (id.HasValue)
            item = await db.Qualifications.SingleAsync(x => x.Id == id.Value);
        else
        {
            item = new Qualification();
            db.Qualifications.Add(item);
        }
        item.Name = name.Trim();
        item.ShortName = shortName.Trim();
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    public async Task<List<EventType>> GetEventTypesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.EventTypes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
    }

    public async Task SaveEventTypeAsync(Guid? id, string name, AttendanceLayout layout, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Der Name des Eventtyps ist erforderlich.");

        await using var db = await factory.CreateDbContextAsync();
        EventType item;
        if (id.HasValue)
            item = await db.EventTypes.SingleAsync(x => x.Id == id.Value);
        else
        {
            item = new EventType();
            db.EventTypes.Add(item);
        }
        item.Name = name.Trim();
        item.Layout = layout;
        item.SortOrder = sortOrder;
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }
}
