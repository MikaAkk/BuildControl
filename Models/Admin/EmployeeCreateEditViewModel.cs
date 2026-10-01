using System.ComponentModel.DataAnnotations;
using WebBuild.Models.Enities;

namespace WebBuild.Models.Admin;

public class EmployeeCreateEditViewModel
{
    public long Id { get; set; }

    [Required] public string Surname { get; set; } = "";
    [Required] public string Name { get; set; } = "";
    public string Patronymic { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string? Password { get; set; } = "";

    public long PositionId { get; set; }
    public long RoleId { get; set; }
    public long EmployeeStateId { get; set; }

    [Required(ErrorMessage = "Номер телефона обязателен")]
    public string PhoneNumberInput { get; set; } = "";
}

