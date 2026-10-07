using AcxiomCRM.Web.ViewModels;

namespace AcxiomCRM.Web.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string userId, string role, string? period = "All");
}
