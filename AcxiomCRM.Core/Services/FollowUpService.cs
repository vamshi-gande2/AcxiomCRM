using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public string? ValidateFollowUpBusinessRules(FollowUp followUp, bool isNew = false)
    {
        // Follow-Up Date Cannot be earlier than today for a new/planned follow-up
        if ((isNew || followUp.Status == "Planned") && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return "Follow-up date cannot be earlier than today.";
        }
        return null;
    }

    public async Task<IEnumerable<FollowUp>> GetFollowUpsAsync(string userId, string role, string? search = null, string? status = null, DateTime? date = null)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedToUser)
            .AsNoTracking()
            .AsQueryable();

        if (role == "SalesExecutive")
        {
            query = query.Where(f => f.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        if (date.HasValue)
        {
            query = query.Where(f => f.FollowUpDate.Date == date.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(f =>
                f.Subject.ToLower().Contains(term) ||
                (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
        }

        return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<FollowUp?> GetFollowUpByIdAsync(int id, string userId, string role)
    {
        var item = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedToUser)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

        if (item == null) return null;

        if (role == "SalesExecutive" && item.AssignedTo != userId)
        {
            return null;
        }

        return item;
    }

    public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string userId)
    {
        var validationError = ValidateFollowUpBusinessRules(followUp, isNew: true);
        if (validationError != null)
        {
            return (false, validationError, null);
        }

        if (string.IsNullOrWhiteSpace(followUp.AssignedTo))
        {
            followUp.AssignedTo = userId;
        }

        followUp.CreatedDate = DateTime.UtcNow;

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Create", "FollowUp", followUp.FollowUpId.ToString(),
            null, $"Created follow-up: {followUp.Subject} for {followUp.FollowUpDate:d}");

        return (true, null, followUp);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateFollowUpAsync(FollowUp followUp, string userId, string role)
    {
        var existing = await _context.FollowUps.FindAsync(followUp.FollowUpId);
        if (existing == null)
        {
            return (false, "Follow-up not found.");
        }

        if (role == "SalesExecutive" && existing.AssignedTo != userId)
        {
            return (false, "Unauthorized: You can only edit your own follow-ups.");
        }

        if (existing.Status == "Planned" && followUp.Status == "Planned")
        {
            var validationError = ValidateFollowUpBusinessRules(followUp, isNew: false);
            if (validationError != null)
            {
                return (false, validationError);
            }
        }

        var oldSummary = $"Subject: {existing.Subject}, Status: {existing.Status}, Date: {existing.FollowUpDate:d}";
        var newSummary = $"Subject: {followUp.Subject}, Status: {followUp.Status}, Date: {followUp.FollowUpDate:d}";

        existing.Subject = followUp.Subject;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.FollowUpType = followUp.FollowUpType;
        existing.Remarks = followUp.Remarks;
        existing.Status = followUp.Status;
        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;

        if (role != "SalesExecutive" && !string.IsNullOrWhiteSpace(followUp.AssignedTo))
        {
            existing.AssignedTo = followUp.AssignedTo;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Update", "FollowUp", followUp.FollowUpId.ToString(),
            oldSummary, newSummary);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string userId, string role, string? remarks = null)
    {
        var existing = await _context.FollowUps.FindAsync(id);
        if (existing == null)
        {
            return (false, "Follow-up not found.");
        }

        if (role == "SalesExecutive" && existing.AssignedTo != userId)
        {
            return (false, "Unauthorized.");
        }

        var oldStatus = existing.Status;
        existing.Status = "Completed";
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = (existing.Remarks ?? "") + " | Completed: " + remarks;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Update", "FollowUp", id.ToString(),
            $"Status: {oldStatus}", "Status: Completed");

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteFollowUpAsync(int id, string userId, string role)
    {
        var existing = await _context.FollowUps.FindAsync(id);
        if (existing == null)
        {
            return (false, "Follow-up not found.");
        }

        if (role == "SalesExecutive")
        {
            return (false, "Unauthorized: Sales executives cannot delete follow-up records.");
        }

        _context.FollowUps.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Delete", "FollowUp", id.ToString(),
            $"Subject: {existing.Subject}", "Deleted follow-up");

        return (true, null);
    }
}
