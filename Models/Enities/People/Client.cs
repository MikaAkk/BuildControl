using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace WebBuild.Models.Enities;

//8. Клиенты
[Table("clients")]
public class Client
{
    [Key]
    public long Id { get; set; }

    [Column("contragents_id")]
    public long? ContragentId { get; set; }
    public virtual Contragent? Contragent { get; set; }

    [Column("people_id")]
    [Required]
    public long PersonDataId { get; set; }
    public virtual PersonData PersonData { get; set; } = null!;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}