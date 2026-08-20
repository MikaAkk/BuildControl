using WebBuild.Models.Enities;

namespace WebBuild.Models.Admin;
public class ApplicationViewModel
    {
        public long Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email {  get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal TotalPrice { get; set; }

        public string? CurrentStatusName { get; set; }
        public long? AssignedManagerId { get; set; }
        public string? AssignedManagerName { get; set; } 

        public IEnumerable<Employee> Managers { get; set; } = Enumerable.Empty<Employee>();
    }
