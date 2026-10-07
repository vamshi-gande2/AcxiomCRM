using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpsApiController(IFollowUpService followUpService, UserManager<ApplicationUser> userManager)
    {
        _followUpService = followUpService;
        _userManager = userManager;
    }

    private async Task<(string UserId, string Role)> GetCurrentUserInfoAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        return (userId, roles.FirstOrDefault() ?? "SalesExecutive");
    }

    private static FollowUpDto ToDto(FollowUp f) => new()
    {
        FollowUpId = f.FollowUpId,
        CustomerId = f.CustomerId,
        CustomerName = f.Customer?.CustomerName,
        LeadId = f.LeadId,
        LeadName = f.Lead?.LeadName,
        Subject = f.Subject,
        FollowUpDate = f.FollowUpDate,
        FollowUpType = f.FollowUpType,
        Remarks = f.Remarks,
        Status = f.Status,
        CreatedDate = f.CreatedDate,
        AssignedToName = f.AssignedToUser?.FullName ?? f.AssignedToUser?.UserName
    };

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] DateTime? date = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var followUps = await _followUpService.GetFollowUpsAsync(userId, role, search, status, date);
        var dtos = followUps.Select(ToDto).ToList();
        return Ok(ApiResponse<List<FollowUpDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFollowUpRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid follow-up data",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var (userId, _) = await GetCurrentUserInfoAsync();

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Subject = dto.Subject,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Remarks = dto.Remarks,
            Status = "Planned",
            AssignedTo = !string.IsNullOrWhiteSpace(dto.AssignedTo) ? dto.AssignedTo : userId
        };

        var (success, errorMessage, created) = await _followUpService.CreateFollowUpAsync(followUp, userId);
        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(errorMessage ?? "Could not create follow-up"));
        }

        return StatusCode(201, ApiResponse<FollowUpDto>.Ok(ToDto(created!), "Follow-up created successfully"));
    }
}
