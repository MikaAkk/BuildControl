using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

[Table("services")]
public class WorkerService
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")] 
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("unit")]
    [MaxLength(50)]
    public string? Unit { get; set; } 

    [Column("base_price")]
    [Required]
    public decimal BasePrice { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    public virtual ICollection<ApplicationService> ApplicationServices { get; set; } = new List<ApplicationService>();
}
