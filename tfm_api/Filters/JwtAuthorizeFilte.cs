using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace tfm_web.Filters
{
    public class JwtAuthorizeFilte : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            //  Not authenticated  token missing / invalid
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult(); // 401
                return;
            }

            // Role check (Admin only)
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            if (role != "Admin")
            {
                context.Result = new ForbidResult(); // 403
            }
        }
    }
}
