using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

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
    public virtual ICollection<EmployeeObjectAssignment> EmployeeAssignments { get; set; } = new HashSet<EmployeeObjectAssignment>();
}

