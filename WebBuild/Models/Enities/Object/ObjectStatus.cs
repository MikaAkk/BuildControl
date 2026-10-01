using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

// --- СТАТУСЫ ОБЪЕКТОВ ---
[Table("object_statuses")]
public class ObjectStatus
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<RealEstateObject> Objects { get; set; } = new HashSet<RealEstateObject>();
}