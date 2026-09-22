using FireDepartmentMvp.Data;
using FireDepartmentMvp.Domain;
using Microsoft.EntityFrameworkCore;

namespace FireDepartmentMvp.Services;

public class AttendanceService(IDbContextFactory<AppDbContext> factory, AppState state)
{
    public async Task<List<FirefighterGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.FirefighterGroups.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Firefighter>> GetFirefightersAsync(Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.Firefighters.AsNoTracking()
            .Include(x => x.Group)
            .Include(x => x.Qualifications)
                .ThenInclude(x => x.Qualification)
            .Where(x => x.IsActive);

        if (groupId.HasValue)
            query = query.Where(x => x.GroupId == groupId.Value);

        return await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(cancellationToken);
    }

    public async Task<List<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Vehicles.AsNoTracking()
            .Include(x => x.Seats.Where(s => s.IsActive))
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Attendance>> GetAttendancesAsync(Guid activityId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Attendances.AsNoTracking()
            .Include(x => x.Firefighter).ThenInclude(x => x.Group)
            .Include(x => x.Firefighter).ThenInclude(x => x.Qualifications).ThenInclude(x => x.Qualification)
            .Include(x => x.Vehicle)
            .Include(x => x.VehicleSeat)
            .Where(x => x.ActivityId == activityId)
            .OrderBy(x => x.Firefighter.LastName)
            .ThenBy(x => x.Firefighter.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task SetEventAttendanceAsync(Guid activityId, Guid firefighterId, AttendanceStatus status, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        if (status is not (AttendanceStatus.Present or AttendanceStatus.Excused))
            throw new InvalidOperationException("Für Events ist nur Anwesend oder Entschuldigt zulässig.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var activity = await db.Activities.SingleAsync(x => x.Id == activityId, cancellationToken);
        EnsureOpen(activity);

        var attendance = await db.Attendances.SingleOrDefaultAsync(x => x.ActivityId == activityId && x.FirefighterId == firefighterId, cancellationToken);
        if (attendance is null)
        {
            attendance = new Attendance
            {
                ActivityId = activityId,
                FirefighterId = firefighterId,
                Status = status,
                ChangedBy = changedBy,
                RegisteredAt = DateTimeOffset.Now
            };
            db.Attendances.Add(attendance);
        }
        else
        {
            attendance.Status = status;
            attendance.VehicleId = null;
            attendance.VehicleSeatId = null;
            attendance.ChangedBy = changedBy;
            attendance.UpdatedAt = DateTimeOffset.Now;
        }

        await db.SaveChangesAsync(cancellationToken);
        state.NotifyChanged();
    }

    public async Task SetMissionAttendanceAsync(Guid activityId, Guid firefighterId, AttendanceStatus status, Guid? vehicleId, Guid? seatId, CancellationToken cancellationToken = default)
    {
        if (status is not (AttendanceStatus.Responded or AttendanceStatus.Standby))
            throw new InvalidOperationException("Für Einsätze ist nur Ausgerückt oder Bereitstellung zulässig.");

        if (status == AttendanceStatus.Responded && vehicleId is null)
            throw new InvalidOperationException("Bei Ausgerückt muss ein Fahrzeug gewählt werden.");

        if (status == AttendanceStatus.Standby)
        {
            vehicleId = null;
            seatId = null;
        }

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var activity = await db.Activities.SingleAsync(x => x.Id == activityId, cancellationToken);
        EnsureOpen(activity);

        if (seatId.HasValue)
        {
            var seat = await db.VehicleSeats.SingleAsync(x => x.Id == seatId.Value, cancellationToken);
            if (seat.VehicleId != vehicleId)
                throw new InvalidOperationException("Der Sitzplatz gehört nicht zum gewählten Fahrzeug.");

            var occupied = await db.Attendances.AnyAsync(x => x.ActivityId == activityId && x.VehicleSeatId == seatId && x.FirefighterId != firefighterId, cancellationToken);
            if (occupied)
                throw new InvalidOperationException("Der Sitzplatz wurde inzwischen von jemand anderem belegt.");
        }

        var attendance = await db.Attendances.SingleOrDefaultAsync(x => x.ActivityId == activityId && x.FirefighterId == firefighterId, cancellationToken);
        if (attendance is null)
        {
            attendance = new Attendance
            {
                ActivityId = activityId,
                FirefighterId = firefighterId,
                Status = status,
                VehicleId = vehicleId,
                VehicleSeatId = seatId,
                RegisteredAt = DateTimeOffset.Now
            };
            db.Attendances.Add(attendance);
        }
        else
        {
            attendance.Status = status;
            attendance.VehicleId = vehicleId;
            attendance.VehicleSeatId = seatId;
            attendance.UpdatedAt = DateTimeOffset.Now;
        }

        await db.SaveChangesAsync(cancellationToken);
        state.NotifyChanged();
    }

    public async Task RemoveAsync(Guid activityId, Guid firefighterId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var activity = await db.Activities.SingleAsync(x => x.Id == activityId, cancellationToken);
        EnsureOpen(activity);

        var attendance = await db.Attendances.SingleOrDefaultAsync(x => x.ActivityId == activityId && x.FirefighterId == firefighterId, cancellationToken);
        if (attendance is null)
            return;

        db.Attendances.Remove(attendance);
        await db.SaveChangesAsync(cancellationToken);
        state.NotifyChanged();
    }

    private static void EnsureOpen(Activity activity)
    {
        if (activity.Status != ActivityStatus.Open)
            throw new InvalidOperationException("Der Vorgang ist bereits geschlossen.");
    }
}
