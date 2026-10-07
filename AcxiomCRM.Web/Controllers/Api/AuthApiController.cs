using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid login request", 
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            await _auditService.LogAsync("Unknown", "Failed Login", "API Auth", null, null,
                $"API Login failed for {model.Email}", HttpContext.Connection.RemoteIpAddress?.ToString());
            return Unauthorized(ApiResponse<object>.Fail("Invalid credentials."));
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            await _auditService.LogAsync(user.Id, "Login", "API Auth", user.Id, null,
                $"User {user.Email} logged in via API", HttpContext.Connection.RemoteIpAddress?.ToString());

            var response = new LoginResponseDto
            {
                UserId = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? "SalesExecutive"
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Login successful."));
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user.Id, "Security", "API Auth", user.Id, null,
                $"API Account locked out: {user.Email}", HttpContext.Connection.RemoteIpAddress?.ToString());
            return StatusCode(403, ApiResponse<object>.Fail("Account is locked out due to multiple failed login attempts."));
        }

        await _auditService.LogAsync(user.Id, "Failed Login", "API Auth", user.Id, null,
            $"API Failed login attempt: {user.Email}", HttpContext.Connection.RemoteIpAddress?.ToString());

        return Unauthorized(ApiResponse<object>.Fail("Invalid credentials."));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            await _auditService.LogAsync(user.Id, "Logout", "API Auth", user.Id, null,
                $"User {user.Email} logged out via API", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        await _signInManager.SignOutAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully."));
    }
}
