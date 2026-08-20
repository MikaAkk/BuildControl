using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

// --- СТАТУСЫ ОБЪЕКТОВ ---
[Table("object_statuses")]
public class ObjectStatus
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<RealEstateObject> Objects { get; set; } = new HashSet<RealEstateObject>();
}


// --- ОБЪЕКТЫ 
[Table("objects")]
public class RealEstateObject
{
    [Key]
    public long Id { get; set; }

    [Column("address")]
    [Required]
    public string Address { get; set; } = string.Empty;

    [Column("project_description")]
    public string? ProjectDescription { get; set; }

    [Column("current_status_id")]
    public long CurrentStatusId { get; set; }
    public virtual ObjectStatus CurrentStatus { get; set; } = null!;
    [Column("manager_employee_id")]
    public long? ManagerEmployeeId { get; set; }
    public virtual Employee? Manager { get; set; }
    [Column("contract_id")]
    public long? ContractId { get; set; } 
    public virtual Contract? Contract { get; set; } 

    public virtual ICollection<WorkTask> Tasks { get; set; } = new HashSet<WorkTask>();
    public virtual ICollection<Document> Documents { get; set; } = new HashSet<Document>();
    public virtual ICollection<ManagersHistory> ManagersHistory { get; set; } = new HashSet<ManagersHistory>();
}


// --- ИСТОРИЯ МЕНЕДЖЕРОВ ---
[Table("managers_history")]
public class ManagersHistory
{
    [Key]
    public long Id { get; set; }

    [Column("object_id")]
    [Required]
    public long ObjectId { get; set; }
    public virtual RealEstateObject Object { get; set; } = null!;

    [Column("manager_employee_id")]
    [Required]
    public long ManagerEmployeeId { get; set; }
    public virtual Employee Manager { get; set; } = null!;
    [Column("assigned_by_employee_id")]
    [Required]
    public long AssignedByEmployeeId { get; set; }
    public virtual Employee AssignedBy { get; set; } = null!;

    [Column("start_date")]
    [Required]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; } 
}
