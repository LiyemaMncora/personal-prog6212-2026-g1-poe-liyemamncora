using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public SessionAuthorizeAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ISession session = context.HttpContext.Session;
        await session.LoadAsync();

        int? userId = session.GetInt32(SessionKeys.UserId);
        string? role = session.GetString(SessionKeys.UserRole);

        if (userId == null || role == null)
        {
            context.Result = new ObjectResult(new { message = "You must log in first." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(role))
        {
            context.Result = new ObjectResult(new { message = $"Your role ({role}) is not allowed to use this endpoint." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}