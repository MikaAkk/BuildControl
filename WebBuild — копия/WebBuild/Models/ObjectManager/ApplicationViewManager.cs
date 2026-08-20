namespace WebBuild.Models.ObjectManager;
public class ApplicationViewManager
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ClientFullName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;

    public List<string> ServiceNames { get; set; } = new List<string>();
    public decimal TotalPrice { get; set; }

    public string StatusName { get; set; } = string.Empty;
}

