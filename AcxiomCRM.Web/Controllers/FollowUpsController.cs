using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpsController(
        IFollowUpService followUpService,
        ICustomerService customerService,
        ILeadService leadService,
        UserManager<ApplicationUser> userManager)
    {
        _followUpService = followUpService;
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

    private async Task PopulateDropDownsAsync(string userId, string role)
    {
        var customers = await _customerService.GetCustomersAsync(userId, role);
        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");

        var leads = await _leadService.GetLeadsAsync(userId, role);
        ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName");

        if (role != "SalesExecutive")
        {
            var salesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
            ViewBag.SalesUsers = salesUsers;
        }
        ViewBag.UserRole = role;
    }

    public async Task<IActionResult> Index(string? search = null, string? status = null, DateTime? date = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        ViewBag.UserRole = role;

        var followUps = await _followUpService.GetFollowUpsAsync(userId, role, search, status, date);
        return View(followUps);
    }

    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        await PopulateDropDownsAsync(userId, role);

        return View(new FollowUp
        {
            CustomerId = customerId,
            LeadId = leadId,
            FollowUpDate = DateTime.UtcNow.Date.AddDays(1),
            Status = "Planned"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        var businessError = _followUpService.ValidateFollowUpBusinessRules(followUp, isNew: true);
        if (businessError != null)
        {
            ModelState.AddModelError(nameof(followUp.FollowUpDate), businessError);
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(userId, role);
            return View(followUp);
        }

        var (success, errorMessage, createdFollowUp) = await _followUpService.CreateFollowUpAsync(followUp, userId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error creating follow-up.");
            await PopulateDropDownsAsync(userId, role);
            return View(followUp);
        }

        TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var followUp = await _followUpService.GetFollowUpByIdAsync(id, userId, role);
        if (followUp == null)
        {
            TempData["ErrorMessage"] = "Follow-up not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropDownsAsync(userId, role);
        return View(followUp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUp followUp)
    {
        if (id != followUp.FollowUpId)
        {
            return BadRequest();
        }

        var (userId, role) = await GetCurrentUserInfoAsync();

        if (followUp.Status == "Planned")
        {
            var businessError = _followUpService.ValidateFollowUpBusinessRules(followUp, isNew: false);
            if (businessError != null)
            {
                ModelState.AddModelError(nameof(followUp.FollowUpDate), businessError);
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(userId, role);
            return View(followUp);
        }

        var (success, errorMessage) = await _followUpService.UpdateFollowUpAsync(followUp, userId, role);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error updating follow-up.");
            await PopulateDropDownsAsync(userId, role);
            return View(followUp);
        }

        TempData["SuccessMessage"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage) = await _followUpService.CompleteFollowUpAsync(id, userId, role, remarks);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up marked as Completed.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage) = await _followUpService.DeleteFollowUpAsync(id, userId, role);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
