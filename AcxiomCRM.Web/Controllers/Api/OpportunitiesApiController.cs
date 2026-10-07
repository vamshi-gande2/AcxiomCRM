using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesApiController(IOpportunityService opportunityService, UserManager<ApplicationUser> userManager)
    {
        _opportunityService = opportunityService;
        _userManager = userManager;
    }

    private async Task<(string UserId, string Role)> GetCurrentUserInfoAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        return (userId, roles.FirstOrDefault() ?? "SalesExecutive");
    }

    private static OpportunityDto ToDto(Opportunity o) => new()
    {
        OpportunityId = o.OpportunityId,
        OpportunityName = o.OpportunityName,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.CustomerName,
        LeadId = o.LeadId,
        Amount = o.Amount,
        Stage = o.Stage,
        Probability = o.Probability,
        WeightedAmount = o.WeightedAmount,
        ExpectedCloseDate = o.ExpectedCloseDate,
        Status = o.Status,
        CreatedDate = o.CreatedDate,
        AssignedToName = o.AssignedToUser?.FullName ?? o.AssignedToUser?.UserName,
        Notes = o.Notes
    };

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? stage = null, [FromQuery] string? status = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var opps = await _opportunityService.GetOpportunitiesAsync(userId, role, search, stage, status);
        var dtos = opps.Select(ToDto).ToList();
        return Ok(ApiResponse<List<OpportunityDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid opportunity data",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var (userId, _) = await GetCurrentUserInfoAsync();

        var opportunity = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Notes = dto.Notes,
            AssignedTo = !string.IsNullOrWhiteSpace(dto.AssignedTo) ? dto.AssignedTo : userId
        };

        var (success, errorMessage, created) = await _opportunityService.CreateOpportunityAsync(opportunity, userId);
        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(errorMessage ?? "Could not create opportunity"));
        }

        return StatusCode(201, ApiResponse<OpportunityDto>.Ok(ToDto(created!), "Opportunity created successfully"));
    }
}
