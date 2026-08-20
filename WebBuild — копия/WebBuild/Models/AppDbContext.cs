using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using WebBuild.Models.Enities;

namespace WebBuild.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    #region DbSets
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();
    public DbSet<PersonData> PersonData => Set<PersonData>();
    public DbSet<EmployeeStat> EmployeeStat => Set<EmployeeStat>();

    public DbSet<Contragent> Contragents => Set<Contragent>();
    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ApplicationStatus> ApplicationStatuses => Set<ApplicationStatus>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<WorkerService> WorkerServices => Set<WorkerService>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<ApplicationService> ApplicationServices => Set<ApplicationService>();
    public DbSet<EmployeeHierarchy> EmployeeHierarchies { get; set; }

    public DbSet<RealEstateObject> RealEstateObjects => Set<RealEstateObject>();
    public DbSet<ObjectStatus> ObjectStatuses => Set<ObjectStatus>();
    public DbSet<ManagersHistory> ManagersHistories => Set<ManagersHistory>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<WorkTaskStatus> WorkTaskStatuses => Set<WorkTaskStatus>();

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<EmailQueue> EmailQueues => Set<EmailQueue>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Если так не сделать, то выйдает ошибку на счет Id не найден
        modelBuilder.Entity<Role>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<Employee>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<Position>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<PhoneNumber>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<PersonData>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<EmployeeStat>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<Contragent>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<Client>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<ApplicationStatus>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<Application>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<ApplicationStatusHistory>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<ApplicationService>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<EmployeeHierarchy>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<RealEstateObject>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<ObjectStatus>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<ManagersHistory>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<WorkTask>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<WorkTaskStatus>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<Document>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<EmailQueue>(e => e.Property(x => x.Id).HasColumnName("id"));
        modelBuilder.Entity<WorkerService>(e => e.Property(x => x.Id).HasColumnName("id"));


        modelBuilder.Entity<WorkTask>(entity =>
        {
            entity.HasOne(t => t.ParentTask)
                  .WithMany(t => t.SubTasks)
                  .HasForeignKey(t => t.ParentTaskId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Object)
                  .WithMany(o => o.Tasks)
                  .HasForeignKey(t => t.ObjectId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.Employee)
                  .WithMany() 
                  .HasForeignKey(t => t.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Status)
                  .WithMany(s => s.Tasks)
                  .HasForeignKey(t => t.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.HasOne(t => t.Employee)
                  .WithMany(e => e.Tasks)
                  .HasForeignKey(t => t.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasOne(a => a.Client)
                  .WithMany(c => c.Applications)
                  .HasForeignKey(a => a.ClientId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.Status)
                  .WithMany(s => s.Applications)
                  .HasForeignKey(a => a.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.AssignedManager)
                  .WithMany(e => e.Applications) 
                  .HasForeignKey(a => a.AssignedManagerId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(a => a.CreatedByEmployee)
                  .WithMany()
                  .HasForeignKey(a => a.CreatedByEmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.UpdatedByEmployee)
                  .WithMany()
                  .HasForeignKey(a => a.UpdatedByEmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(a => a.ApplicationServices)
                  .WithOne(asv => asv.Application)
                  .HasForeignKey(asv => asv.ApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(a => a.StatusHistory)
                  .WithOne(h => h.Application)
                  .HasForeignKey(h => h.ApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RealEstateObject>(entity =>
        {
            entity.HasOne(o => o.CurrentStatus)
                  .WithMany(s => s.Objects)
                  .HasForeignKey(o => o.CurrentStatusId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Manager)
                  .WithMany()
                  .HasForeignKey(o => o.ManagerEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.Contract)
                  .WithMany()
                  .HasForeignKey(o => o.ContractId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ManagersHistory>(entity =>
        {
            entity.HasOne(h => h.Object)
                  .WithMany(o => o.ManagersHistory)
                  .HasForeignKey(h => h.ObjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.AssignedBy)
                  .WithMany()
                  .HasForeignKey(h => h.AssignedByEmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<WorkerService>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BasePrice)
                  .HasColumnType("numeric(18,2)")
                  .IsRequired();
            entity.HasIndex(e => e.IsActive);
            entity.HasMany(s => s.ApplicationServices)
                  .WithOne(x => x.Service)
                  .HasForeignKey(x => x.ServiceId);
        });
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasOne(d => d.UploadedByEmployee)
                  .WithMany()
                  .HasForeignKey(d => d.UploadedByEmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Object)
                  .WithMany(o => o.Documents)
                  .HasForeignKey(d => d.ObjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<EmailQueue>(entity =>
        {
            entity.HasOne(e => e.CreatedByEmployee)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByEmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PeopleId).HasColumnName("people_id");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.EmployeeStateId).HasColumnName("employee_state_id");
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.Employees)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PersonData)
                  .WithOne(pd => pd.Employee)
                  .HasForeignKey<Employee>(e => e.PeopleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Position)
                  .WithMany(p => p.Employees)
                  .HasForeignKey(e => e.PositionId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.EmployeeStat)
                  .WithMany(es => es.Employees)
                  .HasForeignKey(e => e.EmployeeStateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PersonData>(entity =>
        {
            entity.Property(e => e.PhoneNumberId).HasColumnName("phone_number_id");
            entity.HasOne(e => e.PhoneNumber)
          .WithMany(pn => pn.PersonData)
          .HasForeignKey(e => e.PhoneNumberId)
          .OnDelete(DeleteBehavior.Cascade); 
        });
        modelBuilder.Entity<EmployeeHierarchy>(entity =>
        {
            entity.HasOne(h => h.Supervisor)
                  .WithMany(e => e.SubordinatesLinks)
                  .HasForeignKey(h => h.SupervisorEmployeeId)
                  .OnDelete(DeleteBehavior.Restrict); 
            entity.HasOne(h => h.Subordinate)
                  .WithMany(e => e.MySupervisorLinks) 
                  .HasForeignKey(h => h.SubordinateEmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<EmployeeHierarchy>()
       .Property(e => e.SupervisorEmployeeId).HasColumnName("supervisor_employee_id");
        modelBuilder.Entity<EmployeeHierarchy>()
            .Property(e => e.SubordinateEmployeeId).HasColumnName("subordinate_employee_id");
    }
}