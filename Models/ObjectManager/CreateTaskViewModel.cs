using System.ComponentModel.DataAnnotations;

namespace WebBuild.Models.ObjectManager;

public class CreateTaskViewModel
{
    public long ObjectId { get; set; }
    public string ObjectAddress { get; set; } = "";

    [Required(ErrorMessage = "Выберите сотрудника")]
    public long EmployeeId { get; set; }

    [Required(ErrorMessage = "Введите название задачи")]
    [StringLength(255)]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime? PlannedStartDate { get; set; }

    [Required(ErrorMessage = "Укажите плановый срок окончания")]
    [DataType(DataType.Date)]
    public DateTime? PlannedEndDate { get; set; }

    public List<EmployeeOption> AvailableEmployees { get; set; } = new();
}
