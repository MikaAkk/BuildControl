using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;


// 7. Контрагенты
[Table("contragents")]
public class Contragent
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("address")]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}