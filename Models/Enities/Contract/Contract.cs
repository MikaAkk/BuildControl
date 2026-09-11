using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;
// --- ДОГОВОРЫ ---
[Table("contracts")]
public class Contract
{
    [Key]
    public long Id { get; set; }

    [Column("client_id")]
    public long ClientId { get; set; }
    public virtual Client Client { get; set; } = null!;

    [Column("template_id")]
    public long TemplateId { get; set; }
    public virtual ContractTemplate Template { get; set; } = null!;

    [Column("status_id")]
    public long StatusId { get; set; }
    public virtual ContractStatus Status { get; set; } = null!;

    [Column("created_by_employee_id")]
    public long CreatedByEmployeeId { get; set; }

    [ForeignKey("CreatedByEmployeeId")] 
    public virtual Employee? Creator { get; set; }

    [Column("updated_by_employee_id")]
    public long? UpdatedByEmployeeId { get; set; }

    [ForeignKey("UpdatedByEmployeeId")] 
    public virtual Employee? Updater { get; set; }

    [Column("signed_by_employee_id")]
    public long? SignedByEmployeeId { get; set; }

    [ForeignKey("SignedByEmployeeId")]
    public virtual Employee? Signer { get; set; }

    [Column("signed_date")]
    public DateTime? SignedDate { get; set; }

    [Column("file_path")]
    public string? FilePath { get; set; }

    [Column("start_date")]
    [Required]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }

    [Column("termination_reason")]
    public string? TerminationReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}