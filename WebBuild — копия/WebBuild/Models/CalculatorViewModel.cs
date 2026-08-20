using System.ComponentModel.DataAnnotations;

namespace WebBuild.Models;


    public class CalculatorViewModel
    {
        [Required(ErrorMessage = "Выберите тип работ")]
        public long ServiceId { get; set; }

        public decimal? Quantity { get; set; }

        [Required(ErrorMessage = "Введите ваше имя")]
        [StringLength(100, ErrorMessage = "Слишком длинное имя")]
        public string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите телефон")]
        [Phone]
        public string ClientPhone { get; set; } = string.Empty;
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress]
        public string EmailAddress { get; set; }

        public string? CompanyName { get; set; }

        public string? Comment { get; set; } 
    }
