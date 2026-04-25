using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CampusIdleGoods.Helpers;

namespace CampusIdleGoods.Attributes
{
    /// <summary>
    /// 授权特性 - 要求用户登录
    /// </summary>
    public class AuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!SessionHelper.IsLoggedIn(context.HttpContext.Session))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl });
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// 要求用户已认证特性
    /// </summary>
    public class RequireVerifiedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!SessionHelper.IsLoggedIn(context.HttpContext.Session))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl });
                return;
            }

            if (!SessionHelper.IsVerified(context.HttpContext.Session))
            {
                context.Result = new RedirectToActionResult("Profile", "Account", null);
                context.HttpContext.Items["ErrorMessage"] = "请先完成认证后再进行此操作";
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// 要求管理员权限特性
    /// </summary>
    public class RequireAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!SessionHelper.IsLoggedIn(context.HttpContext.Session))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = returnUrl });
                return;
            }

            if (!SessionHelper.IsAdmin(context.HttpContext.Session))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                context.HttpContext.Items["ErrorMessage"] = "您没有权限访问此页面";
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

