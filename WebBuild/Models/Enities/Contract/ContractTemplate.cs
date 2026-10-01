using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;


// --- ШАБЛОНЫ ДОГОВОРОВ ---
[Table("contract_templates")]
public class ContractTemplate
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("version")]
    [MaxLength(20)]
    public string Version { get; set; } = "1.0";

    [Column("file_path")]
    [Required]
    public string FilePath { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_employee_id")]
    public long CreatedByEmployeeId { get; set; }

    public virtual Employee CreatedByEmployee { get; set; } = null!;
}