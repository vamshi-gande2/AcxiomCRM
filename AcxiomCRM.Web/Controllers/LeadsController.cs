using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsController(ILeadService leadService, UserManager<ApplicationUser> userManager)
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

    public async Task<IActionResult> Index(string? search = null, string? status = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.UserRole = role;

        var leads = await _leadService.GetLeadsAsync(userId, role, search, status);
        return View(leads);
    }

    public async Task<IActionResult> Details(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var lead = await _leadService.GetLeadByIdAsync(id, userId, role);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.UserRole = role;
        return View(lead);
    }

    public async Task<IActionResult> Create()
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        ViewBag.SalesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        ViewBag.UserRole = role;

        return View(new Lead { Status = "New", Priority = "Medium" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        if (!ModelState.IsValid)
        {
            ViewBag.SalesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
            ViewBag.UserRole = role;
            return View(lead);
        }

        var (success, errorMessage, createdLead) = await _leadService.CreateLeadAsync(lead, userId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error creating lead.");
            ViewBag.SalesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
            ViewBag.UserRole = role;
            return View(lead);
        }

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var lead = await _leadService.GetLeadByIdAsync(id, userId, role);
        if (lead == null)
        {
            TempData["ErrorMessage"] = "Lead not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SalesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
        ViewBag.UserRole = role;
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead lead)
    {
        if (id != lead.LeadId)
        {
            return BadRequest();
        }

        var (userId, role) = await GetCurrentUserInfoAsync();

        if (!ModelState.IsValid)
        {
            ViewBag.SalesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
            ViewBag.UserRole = role;
            return View(lead);
        }

        var (success, errorMessage) = await _leadService.UpdateLeadAsync(lead, userId, role);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error updating lead.");
            ViewBag.SalesUsers = await _userManager.GetUsersInRoleAsync("SalesExecutive");
            ViewBag.UserRole = role;
            return View(lead);
        }

        TempData["SuccessMessage"] = "Lead details updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id, bool createOpportunity = true)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage, customer, opportunity) = await _leadService.ConvertLeadAsync(id, userId, role, createOpportunity);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = $"Lead successfully converted into Customer '{customer?.CustomerName}'!" +
            (opportunity != null ? $" Opportunity '{opportunity.OpportunityName}' created." : "");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage) = await _leadService.DeleteLeadAsync(id, userId, role);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Lead deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
