using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Produces("application/json")]
public class RolesController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public RolesController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        List<RoleResponse> roles = await _db.Roles
            .OrderBy(r => r.RoleId)
            .Select(r => new RoleResponse { RoleId = r.RoleId, RoleName = r.RoleName })
            .ToListAsync();

        return Ok(roles);
    }
}