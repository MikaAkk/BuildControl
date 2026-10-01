using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;


[Table("task_statuses")]
public class WorkTaskStatus
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public virtual ICollection<WorkTask> Tasks { get; set; } = new HashSet<WorkTask>();
}
