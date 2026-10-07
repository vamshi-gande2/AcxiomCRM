using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager)
    {
        _opportunityService = opportunityService;
        _customerService = customerService;
        _userManager = userManager;
    }

    private async Task<(string UserId, string Role)> GetCurrentUserInfoAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        return (userId, roles.FirstOrDefault() ?? "SalesExecutive");
    }

    private async Task PopulateDropDownsAsync(string userId, string role, int? selectedCustomerId = null)
    {
        var customers = await _customerService.GetCustomersAsync(userId, role);
        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);

        if (role != "SalesExecutive")
        {
            var salesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
            ViewBag.SalesUsers = salesUsers;
        }
        ViewBag.UserRole = role;
    }

    public async Task<IActionResult> Index(string? search = null, string? stage = null, string? status = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.Status = status;
        ViewBag.UserRole = role;

        var opps = await _opportunityService.GetOpportunitiesAsync(userId, role, search, stage, status);
        return View(opps);
    }

    public async Task<IActionResult> Details(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, role);
        if (opp == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.UserRole = role;
        return View(opp);
    }

    public async Task<IActionResult> Create(int? customerId = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        await PopulateDropDownsAsync(userId, role, customerId);

        var model = new Opportunity
        {
            CustomerId = customerId ?? 0,
            Stage = "Qualification",
            Probability = 20,
            ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(30)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        // Business rule validation per Assessment requirements
        var businessError = _opportunityService.ValidateOpportunityBusinessRules(opportunity);
        if (businessError != null)
        {
            ModelState.AddModelError(string.Empty, businessError);
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(userId, role, opportunity.CustomerId);
            return View(opportunity);
        }

        var (success, errorMessage, createdOpp) = await _opportunityService.CreateOpportunityAsync(opportunity, userId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error creating opportunity.");
            await PopulateDropDownsAsync(userId, role, opportunity.CustomerId);
            return View(opportunity);
        }

        TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, role);
        if (opp == null)
        {
            TempData["ErrorMessage"] = "Opportunity not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropDownsAsync(userId, role, opp.CustomerId);
        return View(opp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity opportunity)
    {
        if (id != opportunity.OpportunityId)
        {
            return BadRequest();
        }

        var (userId, role) = await GetCurrentUserInfoAsync();

        var businessError = _opportunityService.ValidateOpportunityBusinessRules(opportunity);
        if (businessError != null)
        {
            ModelState.AddModelError(string.Empty, businessError);
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(userId, role, opportunity.CustomerId);
            return View(opportunity);
        }

        var (success, errorMessage) = await _opportunityService.UpdateOpportunityAsync(opportunity, userId, role);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error updating opportunity.");
            await PopulateDropDownsAsync(userId, role, opportunity.CustomerId);
            return View(opportunity);
        }

        TempData["SuccessMessage"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage) = await _opportunityService.DeleteOpportunityAsync(id, userId, role);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
