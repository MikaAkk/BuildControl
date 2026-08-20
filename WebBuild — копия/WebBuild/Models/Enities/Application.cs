using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebBuild.Models.Enities;

namespace WebBuild.Models.Enities;
// 10. Статусы заявок 
[Table("application_statuses")]
public class ApplicationStatus
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    public virtual ICollection<ApplicationStatusHistory> HistoryRecords { get; set; } = new List<ApplicationStatusHistory>();
}


// 11. Заявки 
[Table("applications")]
public class Application
{
    [Key]
    [Column("id")]
    public long Id { get; set; }


    [Column("client_id")]
    [Required]
    public long ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;


    [Column("status_id")]
    [Required]
    public long StatusId { get; set; }
    public virtual ApplicationStatus Status { get; set; } = null!;

    [Column("assigned_manager_id")]
    public long? AssignedManagerId { get; set; }

    public virtual Employee? AssignedManager { get; set; }
    [Column("admin_comment")]
    public string? AdminComment { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_employee_id")]
    [Required]
    public long CreatedByEmployeeId { get; set; }

    [Column("updated_by_employee_id")]
    [Required]
    public long UpdatedByEmployeeId { get; set; }

    public virtual Employee CreatedByEmployee { get; set; } = null!;
    public virtual Employee UpdatedByEmployee { get; set; } = null!;

    public virtual ICollection<ApplicationService> ApplicationServices { get; set; } = new List<ApplicationService>();
    public virtual ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new HashSet<ApplicationStatusHistory>();
}


// 12. История заявок 
[Table("application_status_history")]
public class ApplicationStatusHistory
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("application_id")]
    [Required]
    public long ApplicationId { get; set; }

    public virtual Application Application { get; set; } = null!;

    [Column("status_id")]
    public long? StatusId { get; set; } 

    public virtual ApplicationStatus? Status { get; set; } 

    [Column("changed_by_employee_id")]
    [Required]
    public long ChangedByEmployeeId { get; set; }

    public virtual Employee ChangedByEmployee { get; set; } = null!;

    [Column("change_comment")]
    public string ChangeComment { get; set; } = string.Empty;

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}


// 13. Связь заявки и услуг
[Table("application_services")]
public class ApplicationService
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("application_id")]
    [Required]
    public long ApplicationId { get; set; }

    public virtual Application Application { get; set; } = null!;


    [Column("service_id")]
    [Required]
    public long ServiceId { get; set; }

    public virtual WorkerService Service { get; set; } = null!;

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("price_per_unit")]
    public decimal PricePerUnit { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }
}

