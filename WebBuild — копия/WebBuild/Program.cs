using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebBuild.Models; 
using WebBuild.Models.Enities;
using WebBuild.Service; 
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DbConnection");
if (string.IsNullOrEmpty(connection))
{
    throw new InvalidOperationException("Строка подключения 'DbConnection' не найдена!");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connection);
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
#region тестовые данные 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Console.WriteLine("Инициализация справочников и пользователей...");
    if (!db.PhoneNumbers.Any())
    {
        db.PhoneNumbers.Add(new PhoneNumber { Phone = "+79990000001", Description = "Тест 1" });
        db.PhoneNumbers.Add(new PhoneNumber { Phone = "+79990000002", Description = "Тест 2" });
        db.PhoneNumbers.Add(new PhoneNumber { Phone = "+79990000003", Description = "Тест 3" });
        Console.WriteLine(" Добавлены тестовые телефоны");
    }

    await db.SaveChangesAsync();
    Console.WriteLine(" Справочники обновлены.");

    var adminRole = db.Roles.FirstOrDefault(r => r.Name == "Администратор");
    var managerRole = db.Roles.FirstOrDefault(r => r.Name == "Руководитель");
    var employeeRole = db.Roles.FirstOrDefault(r => r.Name == "Сотрудник");
    var position = db.Positions.FirstOrDefault(p => p.Name == "Директор") ?? db.Positions.First();
    var phone = db.PhoneNumbers.First();
    var state = db.EmployeeStat.First();

    if (adminRole == null || managerRole == null || employeeRole == null)
    {
        throw new Exception(" КРИТИЧЕСКАЯ ОШИБКА: Не удалось найти одну из ролей в базе данных!");
    }

    if (!db.PersonData.Any(p => p.Email == "admin@test.com"))
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("123456");
        var person = new PersonData
        {
            Surname = "Admin",
            Name = "Super",
            Patronymic = "One",
            PhoneNumberId = phone.Id,
            Email = "admin@test.com",
            PasswordHash = hash
        };
        db.PersonData.Add(person);
        await db.SaveChangesAsync(); 

        var employee = new Employee
        {
            PeopleId = person.Id,
            RoleId = adminRole.Id,
            PositionId = position.Id,
            EmployeeStateId = state.Id
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        Console.WriteLine(" УСПЕХ: АДМИН ДОБАВЛЕН! (admin@test.com)");
    }
    else
    {
        Console.WriteLine(" Админ уже существует.");
    }
    if (!db.PersonData.Any(p => p.Email == "objectManager@test.com"))
    {
        var hash1 = BCrypt.Net.BCrypt.HashPassword("123456");
        var person1 = new PersonData
        {
            Surname = "Manager",
            Name = "Object",
            Patronymic = "One",
            PhoneNumberId = phone.Id,
            Email = "objectManager@test.com",
            PasswordHash = hash1
        };
        db.PersonData.Add(person1);
        await db.SaveChangesAsync();

        var employee1 = new Employee
        {
            PeopleId = person1.Id,
            RoleId = managerRole.Id,
            PositionId = position.Id,
            EmployeeStateId = state.Id
        };
        db.Employees.Add(employee1);
        await db.SaveChangesAsync();
        Console.WriteLine(" УСПЕХ: РУКОВОДИТЕЛЬ ДОБАВЛЕН! (objectManager@test.com)");
    }
    else
    {
        Console.WriteLine(" Руководитель уже существует.");
    }

    if (!db.PersonData.Any(p => p.Email == "employee@test.com"))
    {
        var hash2 = BCrypt.Net.BCrypt.HashPassword("123456");
        var person2 = new PersonData
        {
            Surname = "Emp",
            Name = "Simple",
            Patronymic = "One",
            PhoneNumberId = phone.Id,
            Email = "employee@test.com",
            PasswordHash = hash2
        };
        db.PersonData.Add(person2);
        await db.SaveChangesAsync();

        var employee2 = new Employee
        {
            PeopleId = person2.Id,
            RoleId = employeeRole.Id, 
            PositionId = position.Id,

            EmployeeStateId = state.Id
        };
        db.Employees.Add(employee2);
        await db.SaveChangesAsync();
        Console.WriteLine("УСПЕХ: СОТРУДНИК ДОБАВЛЕН! (employee@test.com)");
    }
    else
    {
        Console.WriteLine("Сотрудник уже существует.");
    }
}
#endregion

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();