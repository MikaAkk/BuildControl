using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using WebBuild.Models.Enities;
namespace WebBuild.Models.Enities;
//1. Роли
[Table("roles")]
public class Role
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Employee> Employees { get; set; } = new HashSet<Employee>();
}


// 2. Телефоны
[Table("phone_numbers")]
public class PhoneNumber
{
    [Key]
    public long Id { get; set; }

    [Column("phone")]
    [Required, MaxLength(50)]

    public string Phone { get; set; } = string.Empty;

    [Column("description")]
    [Required, MaxLength(255)]
    public string Description { get; set; } = string.Empty;
    public virtual ICollection<PersonData> PersonData { get; set; } = new List<PersonData>();
}

// 3. Позиции
[Table("positions")]
public class Position
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

//4. Люди
[Table("peoples")]
public class PersonData
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("surname")]
    [Required, MaxLength(255)]
    public string Surname { get; set; } = string.Empty;

    [Column("name")] 
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("patronymic")]
    [MaxLength(255)]
    public string Patronymic { get; set; } = string.Empty;
    [Column("phone_number_id")]
    public long? PhoneNumberId { get; set; }
    public virtual PhoneNumber? PhoneNumber { get; set; }
    [Column("email")]
    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty; 

    [Column("password_hash")]
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty; 

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public string FullName => $"{Surname} {Name[0]}.{(Patronymic?.Length > 0 ? Patronymic[0] : "")}.";
    public virtual Employee? Employee { get; set; }
}


// 5. Статусы сотрудников
[Table("employee_states")]
public class EmployeeStat
{
    [Key]
    public long Id { get; set; }

    [Column("state")]
    [Required, MaxLength(255)]
    public string State { get; set; } = string.Empty;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

//6. Соотрудники
[Table("employees")] 
public class Employee
{
    [Key]
    public long Id { get; set; }

    public long PeopleId { get; set; }

    public long PositionId { get; set; }
    public long RoleId { get; set; }
    public long EmployeeStateId { get; set; }  
    public virtual Role Role { get; set; }
    public virtual PersonData PersonData { get; set; }
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
    public virtual Position Position { get; set; }
    public virtual EmployeeStat EmployeeStat { get; set; }
    public virtual ICollection<WorkTask> Tasks { get; set; } = new HashSet<WorkTask>();
    public virtual ICollection<Application> Applications { get; set; } = new HashSet<Application>();
    public virtual ICollection<EmployeeHierarchy> SubordinatesLinks { get; set; } = new List<EmployeeHierarchy>();
    public virtual ICollection<EmployeeHierarchy> MySupervisorLinks { get; set; } = new List<EmployeeHierarchy>();

}

// 7. Контрагенты
[Table("contragents")]
public class Contragent
{
    [Key]
    public long Id { get; set; }

    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("address")]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}

//8. Клиенты
[Table("clients")]
public class Client
{
    [Key]
    public long Id { get; set; }

    [Column("contragents_id")]
    public long? ContragentId { get; set; }
    public virtual Contragent? Contragent { get; set; }

    [Column("people_id")]
    [Required]
    public long PersonDataId { get; set; }
    public virtual PersonData PersonData { get; set; } = null!;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

[Table("employees_hierarchy")]
public class EmployeeHierarchy
{
    [Key]
    public long Id { get; set; }
    public long SupervisorEmployeeId { get; set; }
    public long SubordinateEmployeeId { get; set; }
    [ForeignKey("SupervisorEmployeeId")]
    public Employee Supervisor { get; set; } = null!;
    [ForeignKey("SubordinateEmployeeId")]
    public Employee Subordinate { get; set; } = null!;
}

