using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public interface IActivityService
{
    Task<IEnumerable<Activity>> GetActivitiesAsync(string userId, string role, string? type = null, string? status = null);
    Task<Activity?> GetActivityByIdAsync(int id, string userId, string role);
    Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity, string userId);
    Task<(bool Success, string? ErrorMessage)> UpdateActivityAsync(Activity activity, string userId, string role);
    Task<(bool Success, string? ErrorMessage)> DeleteActivityAsync(int id, string userId, string role);
}
