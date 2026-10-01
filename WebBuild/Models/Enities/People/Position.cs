using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

// 3. Позиции
[Table("positions")]
public class Position
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}