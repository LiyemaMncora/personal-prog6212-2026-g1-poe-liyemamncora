using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

public abstract class BaseApiController : ControllerBase
{
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.UserRole) ?? string.Empty;

    protected ObjectResult Forbidden(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message });
    }
}
