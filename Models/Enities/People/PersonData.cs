using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WebBuild.Models.Enities;

//4. Люди
[Table("peoples")]
public class PersonData
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("surname")]
    [Required, MaxLength(255)]
    public string Surname { get; set; } = string.Empty;

    [Column("name")] 
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("patronymic")]
    [MaxLength(255)]
    public string? Patronymic { get; set; } = string.Empty;
    [Column("phone_number_id")]
    public long? PhoneNumberId { get; set; }
    public virtual PhoneNumber? PhoneNumber { get; set; }
    [Column("email")]
    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty; 

    [Column("password_hash")]
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty; 

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public string FullName => $"{Surname} {Name[0]}.{(Patronymic?.Length > 0 ? Patronymic[0] : "")}.";
    public virtual Employee? Employee { get; set; }
}








