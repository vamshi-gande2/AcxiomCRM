using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public interface IFollowUpService
{
    Task<IEnumerable<FollowUp>> GetFollowUpsAsync(string userId, string role, string? search = null, string? status = null, DateTime? date = null);
    Task<FollowUp?> GetFollowUpByIdAsync(int id, string userId, string role);
    Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateFollowUpAsync(FollowUp followUp, string userId, string role);
    Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string userId, string role, string? remarks = null);
    Task<(bool Success, string? ErrorMessage)> DeleteFollowUpAsync(int id, string userId, string role);
    string? ValidateFollowUpBusinessRules(FollowUp followUp, bool isNew = false);
}
