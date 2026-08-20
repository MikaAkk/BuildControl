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


[Table("tasks")]
public class WorkTask
{
    [Key]
    public long Id { get; set; }

    [Column("parent_task_id")]
    public long? ParentTaskId { get; set; }

    public virtual WorkTask? ParentTask { get; set; }

    public virtual ICollection<WorkTask> SubTasks { get; set; } = new HashSet<WorkTask>();

    [Column("object_id")]
    public long ObjectId { get; set; }
    public virtual RealEstateObject Object { get; set; } = null!;
    [Column("employee_id")]
    public long EmployeeId { get; set; }
    public virtual Employee Employee { get; set; } = null!;
    [Column("status_id")]
    public long StatusId { get; set; }
    public virtual WorkTaskStatus Status { get; set; } = null!;

    [Column("title")]
    [Required, MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("planned_start_date")]
    public DateTime? PlannedStartDate { get; set; }

    [Column("planned_end_date")]
    public DateTime? PlannedEndDate { get; set; }

    [Column("actual_start_date")]
    public DateTime? ActualStartDate { get; set; }

    [Column("actual_end_date")]
    public DateTime? ActualEndDate { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } 
}
