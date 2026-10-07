using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.ViewModels;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private async Task<(string UserId, string Role)> GetCurrentUserInfoAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        return (userId, roles.FirstOrDefault() ?? "SalesExecutive");
    }

    public async Task<IActionResult> Index(string report = "pipeline")
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        var model = new ReportsViewModel
        {
            ActiveReport = report.ToLower()
        };

        // Scoped queries
        var customersQuery = _context.Customers.Include(c => c.CreatedByUser).AsNoTracking().AsQueryable();
        var leadsQuery = _context.Leads.Include(l => l.AssignedToUser).AsNoTracking().AsQueryable();
        var oppsQuery = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedToUser).AsNoTracking().AsQueryable();
        var followUpsQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedToUser).AsNoTracking().AsQueryable();
        var activitiesQuery = _context.Activities.Include(a => a.Customer).Include(a => a.Lead).Include(a => a.AssignedToUser).AsNoTracking().AsQueryable();

        if (role == "SalesExecutive")
        {
            customersQuery = customersQuery.Where(c => c.CreatedBy == userId);
            leadsQuery = leadsQuery.Where(l => l.AssignedTo == userId);
            oppsQuery = oppsQuery.Where(o => o.AssignedTo == userId);
            followUpsQuery = followUpsQuery.Where(f => f.AssignedTo == userId);
            activitiesQuery = activitiesQuery.Where(a => a.AssignedTo == userId);
        }

        switch (model.ActiveReport)
        {
            case "customers":
                model.Customers = await customersQuery.OrderByDescending(c => c.CreatedDate).ToListAsync();
                break;
            case "leads":
                model.Leads = await leadsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
                break;
            case "followups":
                model.FollowUps = await followUpsQuery.OrderBy(f => f.FollowUpDate).ToListAsync();
                break;
            case "opportunities":
                model.Opportunities = await oppsQuery.OrderByDescending(o => o.CreatedDate).ToListAsync();
                break;
            case "conversion":
                var allLeads = await leadsQuery.ToListAsync();
                model.TotalLeadsCount = allLeads.Count;
                model.ConvertedLeadsCount = allLeads.Count(l => l.Status == "Converted");
                model.Leads = allLeads;
                break;
            case "activities":
                model.Activities = await activitiesQuery.OrderByDescending(a => a.ActivityDate).ToListAsync();
                break;
            case "audit":
                if (role == "Admin" || role == "Manager")
                {
                    model.AuditLogs = await _context.AuditLogs.Include(a => a.User).OrderByDescending(a => a.CreatedDate).Take(100).ToListAsync();
                }
                break;
            case "pipeline":
            default:
                var opps = await oppsQuery.ToListAsync();
                model.Opportunities = opps;
                model.TotalPipelineValue = opps.Where(o => o.Status == "Open").Sum(o => o.Amount);
                model.WeightedPipelineValue = opps.Where(o => o.Status == "Open").Sum(o => Math.Round(o.Amount * o.Probability / 100m, 2));
                break;
        }

        ViewBag.UserRole = role;
        return View(model);
    }
}
