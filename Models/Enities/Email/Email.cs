using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBuild.Models.Enities;

public enum EmailStatus
{
    Pending,   
    Sending,   
    Sent,      
    Failed     
}

[Table("email_queue")]
public class EmailQueue
{
    [Key]
    public long Id { get; set; }

    [Column("recipient_email")]
    [Required, MaxLength(255)]
    public string RecipientEmail { get; set; } = string.Empty;

    [Column("subject")]
    [Required, MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Column("body")]
    public string? Body { get; set; }

    [Column("send_status")]
    public string SendStatus { get; set; } = EmailStatus.Pending.ToString();

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [Column("created_by_employee_id")]
    public long? CreatedByEmployeeId { get; set; }
    public virtual Employee? CreatedByEmployee { get; set; }
}
