using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
    {
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

    public async Task<IActionResult> Index(string? search = null, string? status = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.UserRole = role;

        var customers = await _customerService.GetCustomersAsync(userId, role, search, status);
        return View(customers);
    }

    public async Task<IActionResult> Details(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var customer = await _customerService.GetCustomerByIdAsync(id, userId, role);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer not found or you do not have permission to access it.";
            return RedirectToAction(nameof(Index));
        }

        return View(customer);
    }

    public IActionResult Create()
    {
        return View(new Customer { Status = "Active" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        var (success, errorMessage, createdCustomer) = await _customerService.CreateCustomerAsync(customer, userId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error creating customer.");
            return View(customer);
        }

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var customer = await _customerService.GetCustomerByIdAsync(id, userId, role);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId)
        {
            return BadRequest();
        }

        var (userId, role) = await GetCurrentUserInfoAsync();

        if (!ModelState.IsValid)
        {
            return View(customer);
        }

        var (success, errorMessage) = await _customerService.UpdateCustomerAsync(customer, userId, role);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Error updating customer.");
            return View(customer);
        }

        TempData["SuccessMessage"] = "Customer details updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage) = await _customerService.DeleteCustomerAsync(id, userId, role);

        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Customer deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
