using System.ComponentModel.DataAnnotations;

namespace WebBuild.Models.ObjectManager;

public class EditTaskViewModel
{
    public long Id { get; set; }
    public long ObjectId { get; set; }
    public string ObjectAddress { get; set; } = "";

    [Required]
    public long EmployeeId { get; set; }

    [Required, StringLength(255)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }
    public DateTime? PlannedStartDate { get; set; }

    [Required]
    public DateTime? PlannedEndDate { get; set; }

    public long StatusId { get; set; }

    public string CurrentStatusName { get; set; } = "";

    public List<EmployeeOption> AvailableEmployees { get; set; } = new();
    public List<StatusOption> AvailableStatuses { get; set; } = new();
}

