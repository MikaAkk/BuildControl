using System.ComponentModel.DataAnnotations;

namespace WebBuild.Models.ObjectManager;

public class AssignEmployeesViewModel
{
    public long ObjectId { get; set; }
    public string ObjectAddress { get; set; } = "";
    public List<EmployeeOption> AvailableEmployees { get; set; } = new();
    public List<long> SelectedEmployeeIds { get; set; } = new();
    public List<long> AlreadyAssignedIds { get; set; } = new();
}

public class EmployeeOption
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Position { get; set; } = "";
}
