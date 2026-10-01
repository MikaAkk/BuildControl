using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebBuild.Models.Enities;


//6. Соотрудники
[Table("employees")]
public class Employee
{
    [Key]
    public long Id { get; set; }

    public long PeopleId { get; set; }

    public long PositionId { get; set; }
    public long RoleId { get; set; }
    public long EmployeeStateId { get; set; }
    public virtual Role Role { get; set; }
    public virtual PersonData PersonData { get; set; }
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
    public virtual Position Position { get; set; }
    public virtual EmployeeStat EmployeeStat { get; set; }
    public virtual ICollection<WorkTask> Tasks { get; set; } = new HashSet<WorkTask>();
    public virtual ICollection<Application> Applications { get; set; } = new HashSet<Application>();
    public virtual ICollection<EmployeeHierarchy> SubordinatesLinks { get; set; } = new List<EmployeeHierarchy>();
    public virtual ICollection<EmployeeHierarchy> MySupervisorLinks { get; set; } = new List<EmployeeHierarchy>();
    public virtual ICollection<RealEstateObject>? ManagedObjects { get; set; } = new List<RealEstateObject>();

}