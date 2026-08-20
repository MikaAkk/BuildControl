using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebBuild.Service;

namespace WebBuild.Controllers.Admin;

public class AdminController : BaseController
{

    private readonly AuthService _auth;

    public AdminController(AuthService auth) : base(auth)
    {
        _auth = auth;
    }
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var roles = _auth.GetRoles();

        if (!roles.Contains("Администратор"))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }

    public IActionResult Index()
    {
        ViewBag.Title = "Панель администратора";
        return View(); 
    }

    public IActionResult UsersManagement()
    {
        return View(); 
    }

}
    
