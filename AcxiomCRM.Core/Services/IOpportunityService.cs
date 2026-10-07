using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public interface IOpportunityService
{
    Task<IEnumerable<Opportunity>> GetOpportunitiesAsync(string userId, string role, string? search = null, string? stage = null, string? status = null);
    Task<Opportunity?> GetOpportunityByIdAsync(int id, string userId, string role);
    Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity, string userId, string role);
    Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int id, string userId, string role);
    string? ValidateOpportunityBusinessRules(Opportunity opportunity);
}
