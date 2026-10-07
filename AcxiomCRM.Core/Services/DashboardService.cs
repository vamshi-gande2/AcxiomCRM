using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.ViewModels;

namespace AcxiomCRM.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, string role, string? period = "All")
    {
        var vm = new DashboardViewModel
        {
            UserRole = role,
            FilterPeriod = period ?? "All"
        };

        var user = await _userManager.FindByIdAsync(userId);
        vm.UserFullName = user?.FullName ?? user?.UserName ?? "User";

        DateTime? filterDate = period switch
        {
            "Today" => DateTime.UtcNow.Date,
            "Week" => DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek),
            "Month" => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
            _ => null
        };

        // Base queries with role scoping
        var customersQuery = _context.Customers.AsNoTracking().AsQueryable();
        var leadsQuery = _context.Leads.AsNoTracking().AsQueryable();
        var oppsQuery = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
        var followUpsQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().AsQueryable();

        if (role == "SalesExecutive")
        {
            customersQuery = customersQuery.Where(c => c.CreatedBy == userId);
            leadsQuery = leadsQuery.Where(l => l.AssignedTo == userId);
            oppsQuery = oppsQuery.Where(o => o.AssignedTo == userId);
            followUpsQuery = followUpsQuery.Where(f => f.AssignedTo == userId);
        }

        if (filterDate.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate >= filterDate.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filterDate.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= filterDate.Value);
            followUpsQuery = followUpsQuery.Where(f => f.CreatedDate >= filterDate.Value);
        }

        // Calculate KPIs
        vm.TotalCustomers = await customersQuery.CountAsync();
        vm.TotalLeads = await leadsQuery.CountAsync();
        vm.OpenLeads = await leadsQuery.CountAsync(l => l.Status != "Converted" && l.Status != "Lost");

        var oppsList = await oppsQuery.ToListAsync();
        vm.TotalOpportunities = oppsList.Count;
        vm.OpenOpportunities = oppsList.Count(o => o.Status == "Open");
        vm.WonOpportunities = oppsList.Count(o => o.Status == "Won");
        vm.LostOpportunities = oppsList.Count(o => o.Status == "Lost");

        vm.TotalPipelineValue = oppsList.Where(o => o.Status == "Open").Sum(o => o.Amount);
        vm.WeightedPipelineValue = oppsList.Where(o => o.Status == "Open")
            .Sum(o => Math.Round((o.Amount * o.Probability) / 100m, 2));

        vm.PendingFollowUps = await followUpsQuery.CountAsync(f => f.Status == "Planned");

        // 1. Chart: Lead Status
        var leadsList = await leadsQuery.ToListAsync();
        var leadStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
        vm.LeadStatusLabels = leadStatuses.ToList();
        vm.LeadStatusCounts = leadStatuses.Select(s => leadsList.Count(l => l.Status == s)).ToList();

        // 2. Chart: Opportunity Pipeline Stages
        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        vm.OpportunityStageLabels = stages.ToList();
        vm.OpportunityStageAmounts = stages.Select(s => oppsList.Where(o => o.Stage == s).Sum(o => o.Amount)).ToList();

        // 3. Chart: Monthly Sales (last 6 months)
        var months = new List<string>();
        var monthlyAmounts = new List<decimal>();
        var now = DateTime.UtcNow;

        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var monthName = targetMonth.ToString("MMM yyyy");
            months.Add(monthName);

            var monthTotal = oppsList
                .Where(o => o.Status == "Won" && o.CreatedDate.Month == targetMonth.Month && o.CreatedDate.Year == targetMonth.Year)
                .Sum(o => o.Amount);
            monthlyAmounts.Add(monthTotal);
        }

        vm.MonthlySalesLabels = months;
        vm.MonthlySalesValues = monthlyAmounts;

        // Recent items
        vm.UpcomingFollowUps = await followUpsQuery
            .Where(f => f.Status == "Planned")
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        vm.RecentOpportunities = oppsList
            .OrderByDescending(o => o.CreatedDate)
            .Take(5)
            .ToList();

        return vm;
    }
}
