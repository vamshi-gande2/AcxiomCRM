using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public interface ICustomerService
{
    Task<IEnumerable<Customer>> GetCustomersAsync(string userId, string role, string? search = null, string? status = null);
    Task<Customer?> GetCustomerByIdAsync(int id, string userId, string role);
    Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer, string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer, string userId, string role);
    Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(int id, string userId, string role);
    Task<bool> IsEmailUniqueAsync(string email, int? customerId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? customerId = null);
}
