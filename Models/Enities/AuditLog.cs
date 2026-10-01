using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("employee_id")]
    public long? EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee? Employee { get; set; }

    [Column("action")]
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = null!;

    [Column("entity_type")]
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = null!; 

    [Column("entity_id")]
    public long? EntityId { get; set; }

    [Column("details")]
    [MaxLength(2000)]
    public string Details { get; set; } = string.Empty;

    [Column("ip_address")]
    [MaxLength(45)]
    public string IpAddress { get; set; } = "unknown";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
