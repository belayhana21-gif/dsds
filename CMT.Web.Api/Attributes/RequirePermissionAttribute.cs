using CMT.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace CMT.Web.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;

        public RequirePermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public async System.Threading.Tasks.Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var authorizationService = context.HttpContext.RequestServices
                .GetRequiredService<CMT.Application.Interfaces.IAuthorizationService>();

            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Authentication required" });
                return;
            }

            var hasPermission = await authorizationService.HasPermissionAsync(
                context.HttpContext.User, _permission);

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new { message = $"Permission '{_permission}' required" })
                {
                    StatusCode = 403
                };
            }
        }
    }
}