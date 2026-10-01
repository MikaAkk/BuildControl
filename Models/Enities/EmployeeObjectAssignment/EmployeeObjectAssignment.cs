using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

[Table("employee_object_assignments")]
public class EmployeeObjectAssignment
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("object_id")]
    public long ObjectId { get; set; }
    [ForeignKey("ObjectId")]
    public virtual RealEstateObject Object { get; set; } = null!;

    [Column("employee_id")]
    public long EmployeeId { get; set; }
    [ForeignKey("EmployeeId")]
    public virtual Employee Employee { get; set; } = null!;

    [Column("assigned_by_employee_id")]
    public long AssignedByEmployeeId { get; set; }
    [ForeignKey("AssignedByEmployeeId")]
    public virtual Employee AssignedBy { get; set; } = null!;

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    [Column("removed_at")]
    public DateTime? RemovedAt { get; set; }
}
