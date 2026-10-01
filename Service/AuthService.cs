using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;
using static BCrypt.Net.BCrypt;
namespace WebBuild.Service;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContext;

    public AuthService(AppDbContext db, IHttpContextAccessor httpContext)
    {
        _db = db;
        _httpContext = httpContext;
    }

    public async Task<bool> RegisterAsync(string login, string password, string surname, string name, string patronymic, string phoneInput)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var existingPerson = await _db.PersonData.FirstOrDefaultAsync(p => p.Email == login);
            if (existingPerson != null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            string passwordHash = HashPassword(password);

            long phoneId = await GetOrCreatePhoneId(phoneInput);

            var newPerson = new PersonData
            {
                Email = login,
                Surname = surname,
                Name = name,
                Patronymic = patronymic,
                PhoneNumberId = phoneId,

                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            await _db.PersonData.AddAsync(newPerson);
            await _db.SaveChangesAsync();
            var defaultRole = await _db.Roles.FirstOrDefaultAsync()
                              ?? throw new Exception("В базе нет ролей! Создайте хотя бы одну.");

            var defaultPosition = await _db.Positions.FirstOrDefaultAsync()
                                  ?? throw new Exception("В базе нет должностей!");

            var defaultState = await _db.EmployeeStat.FirstOrDefaultAsync()
                               ?? throw new Exception("В базе нет статусов!");

            var newEmployee = new Employee
            {
                PeopleId = newPerson.Id,
                RoleId = defaultRole.Id,
                PositionId = defaultPosition.Id,
                EmployeeStateId = defaultState.Id
            };

            await _db.Employees.AddAsync(newEmployee);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<long?> LoginAsync(string login, string password)
    {
        var employee = await _db.Employees
            .Include(e => e.PersonData) 
            .Include(e => e.Role)     
            .FirstOrDefaultAsync(e => e.PersonData.Email == login);

        if (employee == null) return null;

        bool isValid = Verify(password, employee.PersonData.PasswordHash);
        if (!isValid) return null;

        var session = _httpContext.HttpContext.Session;

        session.Set("EmployeeId", BitConverter.GetBytes(employee.Id));

        session.SetString("EmployeeName", employee.PersonData.FullName);

        var roles = new List<string> { employee.Role.Name };
        session.SetJson("Roles", roles);

        return employee.Id;
    }

    public bool IsAuthenticated()
    {
        var bytes = _httpContext.HttpContext.Session.Get("EmployeeId");
        return bytes != null && bytes.Length > 0;
    }

    public void Logout()
    {
        _httpContext.HttpContext.Session.Clear();
    }

    public List<string> GetRoles()
    {
        return _httpContext.HttpContext.Session.GetJson<List<string>>("Roles") ?? new List<string>();
    }
    private async Task<long> GetOrCreatePhoneId(string inputPhone)
    {
        if (string.IsNullOrWhiteSpace(inputPhone))
            throw new ArgumentException("Номер телефона обязателен");

        string cleanPhone = NormalizePhone(inputPhone);

        var existingPhone = await _db.PhoneNumbers
            .FirstOrDefaultAsync(p => p.Phone == cleanPhone);

        if (existingPhone != null)
        {
            return existingPhone.Id;
        }

        var newPhone = new PhoneNumber
        {
            Phone = cleanPhone,
            Description = "Добавлен при регистрации" 
        };

        _db.PhoneNumbers.Add(newPhone);
        await _db.SaveChangesAsync(); 

        return newPhone.Id;
    }

    private string NormalizePhone(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";
        return phone
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "")
            .Trim();
    }
    public long? GetCurrentUserId()
    {
        var bytes = _httpContext.HttpContext.Session.Get("EmployeeId");
        if (bytes == null || bytes.Length == 0)
            return null;


        if (bytes.Length >= 8)
        {
            return BitConverter.ToInt64(bytes, 0);
        }

        if (bytes.Length == 4)
        {
            return BitConverter.ToInt32(bytes, 0);
        }

        return null;
    }
}