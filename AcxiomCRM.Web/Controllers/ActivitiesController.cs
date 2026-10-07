using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivitiesController(
        IActivityService activityService,
        ICustomerService customerService,
        ILeadService leadService,
        UserManager<ApplicationUser> userManager)
    {
        _activityService = activityService;
        _customerService = customerService;
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

    public async Task<IActionResult> Index(string? type = null, string? status = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        ViewBag.Type = type;
        ViewBag.Status = status;
        ViewBag.UserRole = role;

        var activities = await _activityService.GetActivitiesAsync(userId, role, type, status);
        return View(activities);
    }

    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var customers = await _customerService.GetCustomersAsync(userId, role);
        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", customerId);

        var leads = await _leadService.GetLeadsAsync(userId, role);
        ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", leadId);

        return View(new Activity
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityType = "Call",
            ActivityDate = DateTime.UtcNow,
            Status = "Completed"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity activity)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        if (!ModelState.IsValid)
        {
            var customers = await _customerService.GetCustomersAsync(userId, role);
            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", activity.CustomerId);
            var leads = await _leadService.GetLeadsAsync(userId, role);
            ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", activity.LeadId);
            return View(activity);
        }

        var (success, errorMessage, createdActivity) = await _activityService.CreateActivityAsync(activity, userId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error creating activity.");
            return View(activity);
        }

        TempData["SuccessMessage"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage) = await _activityService.DeleteActivityAsync(id, userId, role);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Activity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
