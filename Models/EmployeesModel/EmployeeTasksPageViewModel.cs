using WebBuild.Models.Enities;

namespace WebBuild.Models.EmployeesModel;

public class EmployeeTasksPageViewModel
{
    public List<EmployeeTaskListViewModel> Tasks { get; set; } = new();
    public List<WorkTaskStatus> AvailableStatuses { get; set; } = new(); 
}