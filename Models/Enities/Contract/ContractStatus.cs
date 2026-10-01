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
