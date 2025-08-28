using ApexPerformance.API.Constants;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Database;

public class ApexPerformanceContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApexPerformanceContext(DbContextOptions<ApexPerformanceContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Administrator> Administrators { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentStatus> AppointmentStatuses { get; set; }
    public DbSet<AppointmentRequest> AppointmentRequests { get; set; }
    public DbSet<AppointmentRequestStatus> AppointmentRequestStatuses { get; set; }
    public DbSet<AppointmentRequestType> AppointmentRequestTypes { get; set; }
    public DbSet<AppointmentType> AppointmentTypes { get; set; }
    public DbSet<BodyMeasurement> BodyMeasurements { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientAppointment> ClientAppointments { get; set; }
    
    public DbSet<ClientRecurringAppointment> ClientRecurringAppointments { get; set; }
    public DbSet<Coach> Coaches { get; set; }
    public DbSet<CoachAppointment> CoachAppointments { get; set; }
    public DbSet<CoachClient> CoachClients { get; set; }
    public DbSet<CoachTimeSlot> CoachTimeSlots { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RecurringAppointment> RecurringAppointments { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Program).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e is
            {
                Entity: BaseEntity, State: EntityState.Added or
                EntityState.Modified or
                EntityState.Deleted
            });

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = DateTimeOffset.UtcNow;
                    entity.CreatedBy = _currentUserService.UserId;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Modified:
                case EntityState.Deleted:
                    break;
                default:
                    throw new Exception(ErrorMessages.SavingError);
            }

            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.UpdatedBy = _currentUserService.UserId;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}