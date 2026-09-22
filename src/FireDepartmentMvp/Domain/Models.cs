namespace FireDepartmentMvp.Domain;

public enum ActivityKind
{
    Mission = 1,
    Event = 2
}

public enum ActivityStatus
{
    Open = 1,
    Closed = 2
}

public enum AttendanceLayout
{
    SingleList = 1,
    FiveGroups = 2
}

public enum AttendanceStatus
{
    Present = 1,
    Responded = 2,
    Standby = 3,
    Excused = 4
}

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ActivityKind Kind { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Open;
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? EventTypeId { get; set; }
    public EventType? EventType { get; set; }
    public MissionDetails? MissionDetails { get; set; }
    public List<Attendance> Attendances { get; set; } = new();
}

public class EventType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public AttendanceLayout Layout { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class MissionDetails
{
    public Guid ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public string? ExternalId { get; set; }
    public int Year { get; set; }
    public int Number { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string? AlarmMessage { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? City { get; set; }
    public DateTimeOffset AlarmedAt { get; set; }

    public string MissionNumber => $"{Number:D3}/{Year}";
    public string Address => string.Join(" ", new[] { Street, HouseNumber }.Where(x => !string.IsNullOrWhiteSpace(x))) +
                             (string.IsNullOrWhiteSpace(City) ? string.Empty : $", {City}");
}

public class FirefighterGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<Firefighter> Members { get; set; } = new();
}

public class Firefighter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid GroupId { get; set; }
    public FirefighterGroup Group { get; set; } = null!;
    public List<FirefighterQualification> Qualifications { get; set; } = new();
    public string DisplayName => $"{LastName}, {FirstName}";
}

public class Qualification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<FirefighterQualification> Firefighters { get; set; } = new();
}

public class FirefighterQualification
{
    public Guid FirefighterId { get; set; }
    public Firefighter Firefighter { get; set; } = null!;
    public Guid QualificationId { get; set; }
    public Qualification Qualification { get; set; } = null!;
    public DateOnly? ValidUntil { get; set; }
}

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string CallSign { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<VehicleSeat> Seats { get; set; } = new();
}

public class VehicleSeat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Attendance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public Guid FirefighterId { get; set; }
    public Firefighter Firefighter { get; set; } = null!;
    public AttendanceStatus Status { get; set; }
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? VehicleSeatId { get; set; }
    public VehicleSeat? VehicleSeat { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? ChangedBy { get; set; }
}

public record IncomingAlarmDto(
    string? ExternalId,
    DateTimeOffset? AlarmTime,
    string Keyword,
    string? Message,
    string? Street,
    string? HouseNumber,
    string? City);
