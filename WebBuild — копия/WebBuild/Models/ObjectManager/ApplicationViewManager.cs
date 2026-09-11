namespace WebBuild.Models.ObjectManager;

public class ApplicationViewManager
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ClientFullName { get; set; } = "";
    public string ClientPhone { get; set; } = "";

    public long? ObjectId { get; set; }
    public bool HasObject { get; set; }

    public string StatusName { get; set; } = "";
    public List<string> ServiceNames { get; set; } = new();

    public decimal TotalPrice { get; set; }

    public bool HasContract { get; set; }
    public long? ContractId { get; set; }
    public string ContractStatusName { get; set; } = "";
}
