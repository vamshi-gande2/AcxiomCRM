using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsApiController(ILeadService leadService, UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
        _userManager = userManager;
    }

    private async Task<(string UserId, string Role)> GetCurrentUserInfoAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        return (userId, roles.FirstOrDefault() ?? "SalesExecutive");
    }

    private static LeadDto ToDto(Lead l) => new()
    {
        LeadId = l.LeadId,
        LeadCode = l.LeadCode,
        LeadName = l.LeadName,
        Email = l.Email,
        Phone = l.Phone,
        CompanyName = l.CompanyName,
        Source = l.Source,
        Status = l.Status,
        Priority = l.Priority,
        ExpectedValue = l.ExpectedValue,
        CreatedDate = l.CreatedDate,
        AssignedToName = l.AssignedToUser?.FullName ?? l.AssignedToUser?.UserName
    };

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? status = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var leads = await _leadService.GetLeadsAsync(userId, role, search, status);
        var dtos = leads.Select(ToDto).ToList();
        return Ok(ApiResponse<List<LeadDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid lead data",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var (userId, _) = await GetCurrentUserInfoAsync();

        var lead = new Lead
        {
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = !string.IsNullOrWhiteSpace(dto.AssignedTo) ? dto.AssignedTo : userId
        };

        var (success, errorMessage, created) = await _leadService.CreateLeadAsync(lead, userId);
        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(errorMessage ?? "Could not create lead"));
        }

        return StatusCode(201, ApiResponse<LeadDto>.Ok(ToDto(created!), "Lead created successfully"));
    }
}
