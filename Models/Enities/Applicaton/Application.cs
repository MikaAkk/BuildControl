using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

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
    [Column("contract_id")]
    public long? ContractId { get; set; }

    public virtual Contract? Contract { get; set; }
    public virtual Employee CreatedByEmployee { get; set; } = null!;
    public virtual Employee UpdatedByEmployee { get; set; } = null!;

    public virtual ICollection<ApplicationService> ApplicationServices { get; set; } = new List<ApplicationService>();
    public virtual ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new HashSet<ApplicationStatusHistory>();
}

