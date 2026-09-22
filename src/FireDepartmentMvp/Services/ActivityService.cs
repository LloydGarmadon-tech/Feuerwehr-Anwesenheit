using FireDepartmentMvp.Data;
using FireDepartmentMvp.Domain;
using Microsoft.EntityFrameworkCore;

namespace FireDepartmentMvp.Services;

public enum MissionImportStatus
{
    Created,
    Updated,
    AlreadyClosed
}

public record MissionImportResult(Activity Activity, MissionImportStatus Status);

public class ActivityService(IDbContextFactory<AppDbContext> factory, AppState state)
{
    public async Task<List<Activity>> GetOpenActivitiesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var activities = await db.Activities
            .AsNoTracking()
            .Include(x => x.EventType)
            .Include(x => x.MissionDetails)
            .Include(x => x.Attendances)
                .ThenInclude(x => x.Firefighter)
            .Where(x => x.Status == ActivityStatus.Open)
            .ToListAsync(cancellationToken);

        // SQLite kann DateTimeOffset nicht in ORDER BY übersetzen.
        // Für die kleine Menge offener Aktivitäten sortieren wir daher im Speicher.
        return activities
            .OrderByDescending(x => x.StartedAt)
            .ToList();
    }

    public async Task<List<EventType>> GetEventTypesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.EventTypes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Activity> CreateEventAsync(Guid eventTypeId, string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Bitte einen Titel eingeben.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var type = await db.EventTypes.SingleAsync(x => x.Id == eventTypeId && x.IsActive, cancellationToken);

        var activity = new Activity
        {
            Kind = ActivityKind.Event,
            EventTypeId = type.Id,
            Title = title.Trim(),
            StartedAt = DateTimeOffset.Now,
            Status = ActivityStatus.Open
        };

        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
        state.NotifyChanged();
        return activity;
    }

    // Kompatibilitätsmethode für den bisherigen JSON-Testendpunkt.
    public async Task<Activity> CreateMissionAsync(IncomingAlarmDto alarm, CancellationToken cancellationToken = default)
        => (await ImportMissionAsync(alarm, cancellationToken)).Activity;

    /// <summary>
    /// Legt einen neuen Einsatz an oder aktualisiert einen bereits offenen Einsatz
    /// mit derselben externen ID. Geschlossene Einsätze werden niemals wieder geöffnet.
    /// </summary>
    public async Task<MissionImportResult> ImportMissionAsync(IncomingAlarmDto alarm, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var externalId = string.IsNullOrWhiteSpace(alarm.ExternalId)
            ? null
            : alarm.ExternalId.Trim();

        if (externalId is not null)
        {
            var existing = await db.Activities
                .Include(x => x.MissionDetails)
                .SingleOrDefaultAsync(
                    x => x.MissionDetails != null && x.MissionDetails.ExternalId == externalId,
                    cancellationToken);

            if (existing is not null)
            {
                if (existing.Status == ActivityStatus.Closed)
                    return new MissionImportResult(existing, MissionImportStatus.AlreadyClosed);

                var details = existing.MissionDetails!;
                existing.Title = string.IsNullOrWhiteSpace(alarm.Keyword) ? existing.Title : alarm.Keyword.Trim();

                if (!string.IsNullOrWhiteSpace(alarm.Keyword))
                    details.Keyword = alarm.Keyword.Trim();
                if (alarm.Message is not null)
                    details.AlarmMessage = alarm.Message.Trim();
                if (alarm.Street is not null)
                    details.Street = alarm.Street.Trim();
                if (alarm.HouseNumber is not null)
                    details.HouseNumber = alarm.HouseNumber.Trim();
                if (alarm.City is not null)
                    details.City = alarm.City.Trim();

                // AlarmTime wird bei Nachträgen nur übernommen, wenn FE2 explizit eine Zeit mitsendet.
                if (alarm.AlarmTime is not null)
                    details.AlarmedAt = alarm.AlarmTime.Value;

                await db.SaveChangesAsync(cancellationToken);
                state.NotifyChanged();
                return new MissionImportResult(existing, MissionImportStatus.Updated);
            }
        }

        var alarmTime = alarm.AlarmTime ?? DateTimeOffset.Now;
        var year = alarmTime.Year;
        var lastNumber = await db.MissionDetails
            .Where(x => x.Year == year)
            .Select(x => (int?)x.Number)
            .MaxAsync(cancellationToken) ?? 0;

        var activity = new Activity
        {
            Kind = ActivityKind.Mission,
            Title = string.IsNullOrWhiteSpace(alarm.Keyword) ? "Einsatz" : alarm.Keyword.Trim(),
            StartedAt = alarmTime,
            Status = ActivityStatus.Open,
            MissionDetails = new MissionDetails
            {
                ExternalId = externalId,
                Year = year,
                Number = lastNumber + 1,
                Keyword = alarm.Keyword.Trim(),
                AlarmMessage = alarm.Message?.Trim(),
                Street = alarm.Street?.Trim(),
                HouseNumber = alarm.HouseNumber?.Trim(),
                City = alarm.City?.Trim(),
                AlarmedAt = alarmTime
            }
        };

        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
        state.NotifyChanged();
        return new MissionImportResult(activity, MissionImportStatus.Created);
    }

    public async Task CloseAsync(Guid activityId, string changedBy, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var activity = await db.Activities.SingleAsync(x => x.Id == activityId, cancellationToken);

        if (activity.Status == ActivityStatus.Closed)
            return;

        activity.Status = ActivityStatus.Closed;
        activity.ClosedAt = DateTimeOffset.Now;
        await db.SaveChangesAsync(cancellationToken);
        state.NotifyChanged();
    }
}
