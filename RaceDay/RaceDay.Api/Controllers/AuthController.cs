using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly RaceDayDbContext _db;

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.DateOfBirth != null && request.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { message = "The date of birth cannot be in the future." });
        }

        string roleName = request.RoleName.Trim().ToLower();
        Role? role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName);
        if (role == null)
        {
            return BadRequest(new { message = "The role must be either Organiser or Participant." });
        }

        string email = request.Email.Trim().ToLower();
        bool emailUsed = await _db.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (emailUsed)
        {
            return Conflict(new { message = "That email address is already registered." });
        }

        var user = new User
        {
            RoleId = role.RoleId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber?.Trim(),
            DateOfBirth = request.DateOfBirth
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "That email address is already registered." });
        }

        var response = new RegisterResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            RoleName = role.RoleName
        };

        return Created($"/api/users/{user.UserId}", response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        string email = request.Email.Trim().ToLower();
        User? user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Wrong email or password." });
        }

        await HttpContext.Session.LoadAsync();
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.UserRole, user.Role.RoleName);

        var response = new LoginResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleName = user.Role.RoleName,
            Message = "Login successful."
        };

        return Ok(response);
    }

    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}