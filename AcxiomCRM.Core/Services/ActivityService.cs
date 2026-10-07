using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ActivityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<IEnumerable<Activity>> GetActivitiesAsync(string userId, string role, string? type = null, string? status = null)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedToUser)
            .AsNoTracking()
            .AsQueryable();

        if (role == "SalesExecutive")
        {
            query = query.Where(a => a.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(a => a.ActivityType == type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        return await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
    }

    public async Task<Activity?> GetActivityByIdAsync(int id, string userId, string role)
    {
        var item = await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedToUser)
            .FirstOrDefaultAsync(a => a.ActivityId == id);

        if (item == null) return null;

        if (role == "SalesExecutive" && item.AssignedTo != userId)
        {
            return null;
        }

        return item;
    }

    public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity, string userId)
    {
        if (string.IsNullOrWhiteSpace(activity.AssignedTo))
        {
            activity.AssignedTo = userId;
        }

        activity.CreatedDate = DateTime.UtcNow;

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Create", "Activity", activity.ActivityId.ToString(),
            null, $"Created activity: {activity.Subject} ({activity.ActivityType})");

        return (true, null, activity);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateActivityAsync(Activity activity, string userId, string role)
    {
        var existing = await _context.Activities.FindAsync(activity.ActivityId);
        if (existing == null)
        {
            return (false, "Activity not found.");
        }

        if (role == "SalesExecutive" && existing.AssignedTo != userId)
        {
            return (false, "Unauthorized: You can only edit your own activities.");
        }

        existing.Subject = activity.Subject;
        existing.ActivityType = activity.ActivityType;
        existing.Description = activity.Description;
        existing.ActivityDate = activity.ActivityDate;
        existing.Status = activity.Status;
        existing.CustomerId = activity.CustomerId;
        existing.LeadId = activity.LeadId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Update", "Activity", activity.ActivityId.ToString(),
            null, $"Updated activity: {activity.Subject}");

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteActivityAsync(int id, string userId, string role)
    {
        var existing = await _context.Activities.FindAsync(id);
        if (existing == null)
        {
            return (false, "Activity not found.");
        }

        if (role == "SalesExecutive")
        {
            return (false, "Unauthorized: Sales executives cannot delete activities.");
        }

        _context.Activities.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Delete", "Activity", id.ToString(),
            $"Subject: {existing.Subject}", "Deleted activity");

        return (true, null);
    }
}
