using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

[Table("documents")]
public class Document
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("file_path")]
    [Required]
    public string FilePath { get; set; } = string.Empty;

    [Column("upload_date")]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow; 

    [Column("uploaded_by_employee_id")]
    public long UploadedByEmployeeId { get; set; }

    public virtual Employee UploadedByEmployee { get; set; } = null!;

    [Column("object_id")]
    public long ObjectId { get; set; }

    public virtual RealEstateObject Object { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }
}
