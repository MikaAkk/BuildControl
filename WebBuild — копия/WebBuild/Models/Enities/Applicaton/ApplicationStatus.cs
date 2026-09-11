using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

// 10. Статусы заявок 
[Table("application_statuses")]
public class ApplicationStatus
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    public virtual ICollection<ApplicationStatusHistory> HistoryRecords { get; set; } = new List<ApplicationStatusHistory>();
}
