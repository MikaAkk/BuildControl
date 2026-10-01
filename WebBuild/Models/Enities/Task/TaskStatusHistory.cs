
namespace WebBuild.Models.Enities;

public class TaskStatusHistory
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public long? OldStatusId { get; set; }
    public long NewStatusId { get; set; }
    public long ChangedByEmployeeId { get; set; }
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }

    public Task? Task { get; set; }
    public TaskStatus? OldStatus { get; set; }
    public TaskStatus? NewStatus { get; set; }
    public Employee? ChangedBy { get; set; }
}
