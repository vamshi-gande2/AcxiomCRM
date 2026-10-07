using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomerService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync(string userId, string role, string? search = null, string? status = null)
    {
        var query = _context.Customers
            .Include(c => c.CreatedByUser)
            .AsNoTracking()
            .AsQueryable();

        // Role-based scoping
        if (role == "SalesExecutive")
        {
            query = query.Where(c => c.CreatedBy == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.Contains(term) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                c.CustomerCode.ToLower().Contains(term));
        }

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id, string userId, string role)
    {
        var customer = await _context.Customers
            .Include(c => c.CreatedByUser)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return null;

        if (role == "SalesExecutive" && customer.CreatedBy != userId)
        {
            return null; // Forbidden for this sales rep
        }

        return customer;
    }

    public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer, string userId)
    {
        // Business Validation: Email & Phone uniqueness
        if (!await IsEmailUniqueAsync(customer.Email))
        {
            return (false, "A customer with this email address already exists.", null);
        }

        if (!await IsPhoneUniqueAsync(customer.Phone))
        {
            return (false, "A customer with this phone number already exists.", null);
        }

        customer.CreatedBy = userId;
        customer.CreatedDate = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
        {
            customer.CustomerCode = $"CUST-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}";
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Create", "Customer", customer.CustomerId.ToString(),
            null, $"Created customer {customer.CustomerName} ({customer.Email})");

        return (true, null, customer);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer, string userId, string role)
    {
        var existing = await _context.Customers.FindAsync(customer.CustomerId);
        if (existing == null)
        {
            return (false, "Customer not found.");
        }

        if (role == "SalesExecutive" && existing.CreatedBy != userId)
        {
            return (false, "Unauthorized: You can only update customers assigned to you.");
        }

        if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
        {
            return (false, "A customer with this email address already exists.");
        }

        if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
        {
            return (false, "A customer with this phone number already exists.");
        }

        var oldSummary = $"Name: {existing.CustomerName}, Email: {existing.Email}, Status: {existing.Status}";
        var newSummary = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        existing.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Update", "Customer", customer.CustomerId.ToString(),
            oldSummary, newSummary);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(int id, string userId, string role)
    {
        var existing = await _context.Customers.FindAsync(id);
        if (existing == null)
        {
            return (false, "Customer not found.");
        }

        if (role == "SalesExecutive")
        {
            return (false, "Unauthorized: Sales executives are not permitted to delete customers.");
        }

        // Soft delete / deactivation or remove
        var oldSummary = $"Customer: {existing.CustomerName} ({existing.CustomerCode})";
        _context.Customers.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Delete", "Customer", id.ToString(),
            oldSummary, "Deleted customer record");

        return (true, null);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? customerId = null)
    {
        var normalized = email.Trim().ToLower();
        return !await _context.Customers.AnyAsync(c =>
            c.Email.ToLower() == normalized &&
            (!customerId.HasValue || c.CustomerId != customerId.Value));
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? customerId = null)
    {
        var normalized = phone.Trim();
        return !await _context.Customers.AnyAsync(c =>
            c.Phone == normalized &&
            (!customerId.HasValue || c.CustomerId != customerId.Value));
    }
}
