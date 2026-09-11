using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

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
