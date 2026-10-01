using System.ComponentModel.DataAnnotations;

namespace WebBuild.Models.Admin;

public class ClientEditViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Фамилия обязательна")]
    public string Surname { get; set; } = "";

    [Required(ErrorMessage = "Имя обязательно")]
    public string Name { get; set; } = "";

    public string ? Patronymic { get; set; } = "";

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string PhoneNumberInput { get; set; } = "";

    public string ? CompanyName { get; set; } = "";
    public string? CompanyAddress { get; set; } = "";
}
