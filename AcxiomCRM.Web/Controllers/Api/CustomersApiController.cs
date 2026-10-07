using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.DTOs;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersApiController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
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

    private static CustomerDto ToDto(Customer c) => new()
    {
        CustomerId = c.CustomerId,
        CustomerCode = c.CustomerCode,
        CustomerName = c.CustomerName,
        Email = c.Email,
        Phone = c.Phone,
        CompanyName = c.CompanyName,
        Address = c.Address,
        City = c.City,
        State = c.State,
        Status = c.Status,
        CreatedDate = c.CreatedDate,
        CreatedByName = c.CreatedByUser?.FullName ?? c.CreatedByUser?.UserName
    };

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? status = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var customers = await _customerService.GetCustomersAsync(userId, role, search, status);
        var dtos = customers.Select(ToDto).ToList();
        return Ok(ApiResponse<List<CustomerDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var customer = await _customerService.GetCustomerByIdAsync(id, userId, role);
        if (customer == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Customer with ID {id} not found or unauthorized."));
        }

        return Ok(ApiResponse<CustomerDto>.Ok(ToDto(customer)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid customer data",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var (userId, _) = await GetCurrentUserInfoAsync();

        // Check uniqueness before create
        if (!await _customerService.IsEmailUniqueAsync(dto.Email))
        {
            return Conflict(ApiResponse<object>.Fail("A customer with this email address already exists."));
        }

        if (!await _customerService.IsPhoneUniqueAsync(dto.Phone))
        {
            return Conflict(ApiResponse<object>.Fail("A customer with this phone number already exists."));
        }

        var customer = new Customer
        {
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status
        };

        var (success, errorMessage, created) = await _customerService.CreateCustomerAsync(customer, userId);
        if (!success)
        {
            return BadRequest(ApiResponse<object>.Fail(errorMessage ?? "Could not create customer"));
        }

        return CreatedAtAction(nameof(GetById), new { id = created!.CustomerId }, ApiResponse<CustomerDto>.Ok(ToDto(created), "Customer created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid customer data",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var (userId, role) = await GetCurrentUserInfoAsync();

        if (!await _customerService.IsEmailUniqueAsync(dto.Email, id))
        {
            return Conflict(ApiResponse<object>.Fail("A customer with this email address already exists."));
        }

        if (!await _customerService.IsPhoneUniqueAsync(dto.Phone, id))
        {
            return Conflict(ApiResponse<object>.Fail("A customer with this phone number already exists."));
        }

        var customer = new Customer
        {
            CustomerId = id,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status
        };

        var (success, errorMessage) = await _customerService.UpdateCustomerAsync(customer, userId, role);
        if (!success)
        {
            if (errorMessage != null && errorMessage.Contains("Unauthorized"))
                return Forbid();

            return BadRequest(ApiResponse<object>.Fail(errorMessage ?? "Update failed"));
        }

        return Ok(ApiResponse<object>.Ok(new { CustomerId = id }, "Customer updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();
        var (success, errorMessage) = await _customerService.DeleteCustomerAsync(id, userId, role);

        if (!success)
        {
            if (errorMessage != null && errorMessage.Contains("Unauthorized"))
                return Forbid();

            return NotFound(ApiResponse<object>.Fail(errorMessage ?? "Delete failed"));
        }

        return Ok(ApiResponse<object>.Ok(new { CustomerId = id }, "Customer deleted successfully."));
    }
}
