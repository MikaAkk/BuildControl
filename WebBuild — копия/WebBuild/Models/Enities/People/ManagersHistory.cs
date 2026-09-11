using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

[Table("managers_history")]
public class ManagersHistory
{
    [Key]
    public long Id { get; set; }

    [Column("object_id")]
    [Required]
    public long ObjectId { get; set; }
    public virtual RealEstateObject Object { get; set; } = null!;

    [Column("manager_employee_id")]
    [Required]
    [ForeignKey(nameof(Manager))]
    public long ManagerEmployeeId { get; set; }
    public virtual Employee Manager { get; set; } = null!;

    [Column("assigned_by_employee_id")]
    [Required]
    [ForeignKey(nameof(AssignedBy))]
    public long AssignedByEmployeeId { get; set; }
    public virtual Employee AssignedBy { get; set; } = null!;

    [Column("start_date")]
    [Required]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime? EndDate { get; set; }
}
