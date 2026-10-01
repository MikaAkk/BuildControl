using System.ComponentModel.DataAnnotations;

namespace WebBuild.Models.ObjectManager;

public class ContractServicesViewModel
{
    public long ContractId { get; set; }
    public long ApplicationId { get; set; }
    public string ClientName { get; set; } = "";
    public string ContractStatusName { get; set; } = "";

    public List<EditableServiceItem> ExistingServices { get; set; } = new();

    public long? NewServiceId { get; set; }
    public decimal? NewQuantity { get; set; }
    public decimal? NewPricePerUnit { get; set; }

    public List<ServiceOption> AvailableServices { get; set; } = new();

    public bool CanEdit => !ContractStatusName.Contains("Подписан")
                        && !ContractStatusName.Contains("Расторгнут");
}

public class EditableServiceItem
{
    public long Id { get; set; }
    public long ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
    public string Unit { get; set; } = "";

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    public decimal PricePerUnit { get; set; }

    public decimal TotalPrice => Quantity * PricePerUnit;
    public bool ShouldDelete { get; set; }
}

public class ServiceOption
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal BasePrice { get; set; }
}
