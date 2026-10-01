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
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string State { get; set; } = "—";
    public int ActiveObjectCount { get; set; }
    public string ObjectsSummary { get; set; } = "";
    public int ActiveTaskCount { get; set; }
    public bool IsOverloaded { get; set; }
}

public class EmployeeObjectStat
{
    public long EmployeeId { get; set; }
    public int Count { get; set; }
    public List<string> Addresses { get; set; } = new List<string>();
}

public class EmployeeTaskStat
{
    public long EmployeeId { get; set; }
    public int Count { get; set; }
}
