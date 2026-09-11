using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;


// 5. Статусы сотрудников
[Table("employee_states")]
public class EmployeeStat
{
    [Key]
    public long Id { get; set; }

    [Column("state")]
    [Required, MaxLength(255)]
    public string State { get; set; } = string.Empty;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
