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
    public DbSet<AppointmentType> AppointmentTypes { get; set; }
    public DbSet<BodyMeasurement> BodyMeasurements { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientAppointment> ClientAppointments { get; set; }
    public DbSet<Coach> Coaches { get; set; }
    public DbSet<CoachAppointment> CoachAppointments { get; set; }
    public DbSet<CoachClient> CoachClients { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Program).Assembly);

        // Seed Users
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = new Guid(SeedData.SuperAdminId),
                CreatedAt = DateTimeOffset.Now,
                CreatedBy = new Guid(SeedData.SuperAdminId),
                UpdatedAt = DateTimeOffset.Now,
                UpdatedBy = new Guid(SeedData.SuperAdminId),
                IsDeleted = false,
                DeletedAt = null,
                UserName = SeedData.SuperAdminUserName,
                Email = SeedData.SuperAdminEmail,
                Password = User.HashPassword(SeedData.SuperAdminPassword),
                EmailConfirmed = true
            });

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                Id = new Guid(SeedData.SuperAdminRoleId),
                CreatedAt = DateTimeOffset.Now,
                CreatedBy = new Guid(SeedData.SuperAdminId),
                UpdatedAt = DateTimeOffset.Now,
                UpdatedBy = new Guid(SeedData.SuperAdminId),
                IsDeleted = false,
                DeletedAt = null,
                Name = SeedData.SuperAdminRoleName,
                Description = SeedData.SuperAdminRoleDescription,
            });

        // Seed UserRole (pivot table)
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                UserId = new Guid(SeedData.SuperAdminId),
                RoleId = new Guid(SeedData.SuperAdminRoleId)
            }
        );

        // Seed Administrator
        modelBuilder.Entity<Administrator>().HasData(new Administrator
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            CreatedBy = new Guid(SeedData.SuperAdminId),
            UpdatedAt = DateTimeOffset.Now,
            UpdatedBy = new Guid(SeedData.SuperAdminId),
            IsDeleted = false,
            DeletedAt = null,
            UserId = new Guid(SeedData.SuperAdminId),
            FirstName = SeedData.SuperAdminFirstName,
            LastName = SeedData.SuperAdminLastName
        });

        // Seed Permissions
        /*var permissions = UserPermissions.GetUserPermissions()
            .Select(permission => new Permission
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Category = permission.Category
            })
            .ToList();

        modelBuilder.Entity<Permission>().HasData(permissions);*/
        
        // Seed Statuses
        var appointmentStatuses = BusinessStatuses.GetBusinessStatuses()
            .Select(status => new AppointmentStatus()
            {
                Id = status.Id,
                Name = status.Name,
                Description = status.Description,
            })
            .ToList();
        
        modelBuilder.Entity<AppointmentStatus>().HasData(appointmentStatuses);

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
                    entity.CreatedAt = DateTimeOffset.Now;
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

            entity.UpdatedAt = DateTimeOffset.Now;
            entity.UpdatedBy = _currentUserService.UserId;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}