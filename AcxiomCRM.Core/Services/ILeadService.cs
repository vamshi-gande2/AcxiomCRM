using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public interface ILeadService
{
    Task<IEnumerable<Lead>> GetLeadsAsync(string userId, string role, string? search = null, string? status = null);
    Task<Lead?> GetLeadByIdAsync(int id, string userId, string role);
    Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead, string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead, string userId, string role);
    Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int id, string userId, string role);
    Task<(bool Success, string? ErrorMessage, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(int leadId, string userId, string role, bool createOpportunity = true);
    Task<bool> IsEmailUniqueAsync(string email, int? leadId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? leadId = null);
}
