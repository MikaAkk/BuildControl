using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;


// --- СТАТУСЫ ДОГОВОРОВ ---
[Table("contract_statuses")]
public class ContractStatus
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

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
    public virtual Employee Creator { get; set; } = null!;

    [Column("updated_by_employee_id")]
    public long? UpdatedByEmployeeId { get; set; }
    public virtual Employee? Updater { get; set; }

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
