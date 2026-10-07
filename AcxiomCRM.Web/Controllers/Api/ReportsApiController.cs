using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        var role = roles.FirstOrDefault() ?? "SalesExecutive";

        var query = _context.Opportunities
            .Include(o => o.AssignedToUser)
            .AsNoTracking()
            .AsQueryable();

        if (role == "SalesExecutive")
        {
            query = query.Where(o => o.AssignedTo == userId);
        }

        var opps = await query.ToListAsync();
        var openOpps = opps.Where(o => o.Status == "Open").ToList();

        var stageGroups = opps
            .GroupBy(o => o.Stage)
            .Select(g => new PipelineStageReportDto
            {
                Stage = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => Math.Round(x.Amount * x.Probability / 100m, 2))
            })
            .ToList();

        var ownerGroups = opps
            .GroupBy(o => o.AssignedToUser?.FullName ?? o.AssignedToUser?.UserName ?? "Unassigned")
            .Select(g => new PipelineOwnerReportDto
            {
                OwnerName = g.Key,
                OpportunitiesCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .ToList();

        var report = new PipelineReportDto
        {
            TotalPipelineValue = openOpps.Sum(o => o.Amount),
            TotalWeightedPipelineValue = openOpps.Sum(o => Math.Round(o.Amount * o.Probability / 100m, 2)),
            TotalOpenOpportunities = openOpps.Count,
            Stages = stageGroups,
            ByOwner = ownerGroups
        };

        return Ok(ApiResponse<PipelineReportDto>.Ok(report));
    }
}
