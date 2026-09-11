using System.ComponentModel.DataAnnotations;

namespace WebBuild.Models.ObjectManager;

public class CreateObjectViewModel
{
    public long ContractId { get; set; }
    public string ClientName { get; set; } = "";
    public string ContractTemplateName { get; set; } = "";

    [Required(ErrorMessage = "Укажите адрес объекта")]
    [Display(Name = "Адрес объекта")]
    public string Address { get; set; } = "";

    [Display(Name = "Описание проекта")]
    public string? ProjectDescription { get; set; }

    [Display(Name = "Начальный статус")]
    public long StatusId { get; set; }

    public List<StatusOption> AvailableStatuses { get; set; } = new();
}

public class StatusOption
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}
