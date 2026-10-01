using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebBuild.Service;

namespace WebBuild.Controllers;

public class BaseController : Controller
{
    protected readonly AuthService _auth;

    public BaseController(AuthService auth)
    {
        _auth = auth;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();
        if (controllerName == "Account" &&
            (actionName == "ShowLogin" || actionName == "DoLogin"))
        {
            return; 
        }
        if (!_auth.IsAuthenticated())
        {
            context.Result = new RedirectToActionResult("ShowLogin", "Account", null);
        }
    }
}
