using FireDepartmentMvp.Domain;
using Microsoft.EntityFrameworkCore;

namespace FireDepartmentMvp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<MissionDetails> MissionDetails => Set<MissionDetails>();
    public DbSet<FirefighterGroup> FirefighterGroups => Set<FirefighterGroup>();
    public DbSet<Firefighter> Firefighters => Set<Firefighter>();
    public DbSet<Qualification> Qualifications => Set<Qualification>();
    public DbSet<FirefighterQualification> FirefighterQualifications => Set<FirefighterQualification>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ActivityVehicle> ActivityVehicles => Set<ActivityVehicle>();
    public DbSet<VehicleSeat> VehicleSeats => Set<VehicleSeat>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityVehicle>()
            .HasKey(x => new { x.ActivityId, x.VehicleId });

        modelBuilder.Entity<ActivityVehicle>()
            .HasOne(x => x.Activity)
            .WithMany()
            .HasForeignKey(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityVehicle>()
            .HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<MissionDetails>()
            .HasKey(x => x.ActivityId);

        modelBuilder.Entity<Activity>()
            .HasOne(x => x.MissionDetails)
            .WithOne(x => x.Activity)
            .HasForeignKey<MissionDetails>(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attendance>()
            .HasIndex(x => new { x.ActivityId, x.FirefighterId })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(x => new { x.ActivityId, x.VehicleSeatId })
            .IsUnique()
            .HasFilter("VehicleSeatId IS NOT NULL");

        modelBuilder.Entity<FirefighterQualification>()
            .HasKey(x => new { x.FirefighterId, x.QualificationId });

        modelBuilder.Entity<FirefighterQualification>()
            .HasOne(x => x.Firefighter)
            .WithMany(x => x.Qualifications)
            .HasForeignKey(x => x.FirefighterId);

        modelBuilder.Entity<FirefighterQualification>()
            .HasOne(x => x.Qualification)
            .WithMany(x => x.Firefighters)
            .HasForeignKey(x => x.QualificationId);
    }
}
