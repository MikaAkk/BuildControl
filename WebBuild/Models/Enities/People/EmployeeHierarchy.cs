using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;


[Table("employees_hierarchy")]
public class EmployeeHierarchy
{
    [Key]
    public long Id { get; set; }
    public long SupervisorEmployeeId { get; set; }
    public long SubordinateEmployeeId { get; set; }
    [ForeignKey("SupervisorEmployeeId")]
    public Employee Supervisor { get; set; } = null!;
    [ForeignKey("SubordinateEmployeeId")]
    public Employee Subordinate { get; set; } = null!;
}