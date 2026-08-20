using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebBuild.Models;
using WebBuild.Service;

namespace WebBuild.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly AuthService _auth;

    public AccountController(AuthService auth)
    {
        _auth = auth;
    }

    // GET: /account/login
    [HttpGet("login")]
    public IActionResult ShowLogin()
    {
        if (_auth.IsAuthenticated())
        {
            var roles = _auth.GetRoles();
            return RedirectToRolePage(roles);
        }
        return View("Login"); 
    }

    // POST: /account/login 
    [HttpPost("login")]
    public async Task<IActionResult> DoLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.DebugInfo = "Ошибка валидации: поля пустые или неверного формата.";
            return View("Login", model);
        }

        Debug.WriteLine($"[LOGIN] Попытка входа для: {model.Email}");
        var id = await _auth.LoginAsync(model.Email, model.Password);

        if (id.HasValue)
        {
            Debug.WriteLine("[LOGIN] Успешный вход в сервис!");
            var roles = _auth.GetRoles();
            string rolesString = roles.Any() ? string.Join(", ", roles) : "(роли не найдены)";
            ViewBag.FinalMessage = $"РОЛИ ПОЛЬЗОВАТЕЛЯ: [{rolesString}]";
            return RedirectToRolePage(roles);
        }
        else
        {
            Debug.WriteLine("[LOGIN] Неверный логин или пароль (или проблема с сессией)");
            ModelState.AddModelError("", "Неверный логин или пароль");
            return View("Login", model);
        }
    }

    private IActionResult RedirectToRolePage(List<string> roles)
    {
        if (roles.Contains("Администратор"))
        {
            return RedirectToAction("Index", "Admin");
        }

        if (roles.Contains("Руководитель") || roles.Contains("Менеджер"))
        {
            return RedirectToAction("Index", "ObjectManager");
        }

        if (roles.Contains("Сотрудник"))
        {
            return RedirectToAction("Index", "Employee");
        }
        return RedirectToAction("Dashboard", "Home");
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _auth.Logout();
        return RedirectToAction("ShowLogin", "Account");
    }
}