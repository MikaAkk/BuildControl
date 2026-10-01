using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

// 2. Телефоны
[Table("phone_numbers")]
public class PhoneNumber
{
    [Key]
    public long Id { get; set; }

    [Column("phone")]
    [Required, MaxLength(50)]

    public string Phone { get; set; } = string.Empty;

    [Column("description")]
    [Required, MaxLength(255)]
    public string Description { get; set; } = string.Empty;
    public virtual ICollection<PersonData> PersonData { get; set; } = new List<PersonData>();
}
