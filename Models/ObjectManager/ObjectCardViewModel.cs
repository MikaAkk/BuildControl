using WebBuild.Models.Enities;

namespace WebBuild.Models.ObjectManager;

public class ObjectCardViewModel
{
    public RealEstateObject Object { get; set; }
    public int TotalTasks { get; set; }
    public int ActiveTasks { get; set; }
    public int DoneTasks { get; set; }
}
