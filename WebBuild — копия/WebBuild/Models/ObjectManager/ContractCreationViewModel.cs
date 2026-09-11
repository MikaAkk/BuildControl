using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WebBuild.Models.Enities;

namespace WebBuild.Models.ObjectManager;

public class ContractCreationViewModel
{
    public long ApplicationId { get; set; }
    public long ClientId { get; set; }
    public string ClientName { get; set; } = "";
    public long SelectedTemplateId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public List<ServiceLineItem> Services { get; set; } = new();
}

public class ServiceLineItem
{
    public long ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total { get; set; }
}

public class ContractEditViewModel
{
    public long Id { get; set; } 
    public string ClientName { get; set; } = "";
    public string ClientEmail { get; set; } = "";
    public string TemplateName { get; set; } = "";
    public string StatusName { get; set; } = "";
    public string SignerName { get; set; } = "";
    public bool IsSigned { get; set; }

    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }

    public bool ShouldSign { get; set; }
    public long SelectedStatusId { get; set; }
    public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();

}
