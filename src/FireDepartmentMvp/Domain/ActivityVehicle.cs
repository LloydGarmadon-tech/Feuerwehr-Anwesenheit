namespace FireDepartmentMvp.Domain;

public class ActivityVehicle
{
    public Guid ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
}
