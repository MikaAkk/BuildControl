using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

// 13. Связь заявки и услуг
[Table("application_services")]
public class ApplicationService
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("application_id")]
    [Required]
    public long ApplicationId { get; set; }

    public virtual Application Application { get; set; } = null!;


    [Column("service_id")]
    [Required]
    public long ServiceId { get; set; }

    public virtual WorkerService Service { get; set; } = null!;

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("price_per_unit")]
    public decimal PricePerUnit { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }
}

