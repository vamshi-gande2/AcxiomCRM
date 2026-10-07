using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OpportunityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public string? ValidateOpportunityBusinessRules(Opportunity opportunity)
    {
        // 1. Amount rule: Must be greater than 0
        if (opportunity.Amount <= 0)
        {
            return "Opportunity Amount must be greater than 0.";
        }

        // 2. Probability rule: Between 0 and 100
        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            return "Probability must be between 0 and 100.";
        }

        // 3. Expected Close Date rule: Cannot be in the past for active opportunities
        var today = DateTime.UtcNow.Date;
        var isActive = opportunity.Stage != "Won" && opportunity.Stage != "Lost" && opportunity.Status == "Open";
        if (isActive && opportunity.ExpectedCloseDate.Date < today)
        {
            return "Expected Close Date cannot be in the past.";
        }

        return null;
    }

    public async Task<IEnumerable<Opportunity>> GetOpportunitiesAsync(string userId, string role, string? search = null, string? stage = null, string? status = null)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedToUser)
            .AsNoTracking()
            .AsQueryable();

        if (role == "SalesExecutive")
        {
            query = query.Where(o => o.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(term) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(int id, string userId, string role)
    {
        var opp = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedToUser)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opp == null) return null;

        if (role == "SalesExecutive" && opp.AssignedTo != userId)
        {
            return null;
        }

        return opp;
    }

    public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string userId)
    {
        var validationError = ValidateOpportunityBusinessRules(opportunity);
        if (validationError != null)
        {
            return (false, validationError, null);
        }

        if (string.IsNullOrWhiteSpace(opportunity.AssignedTo))
        {
            opportunity.AssignedTo = userId;
        }

        // Automatic status synchronisation based on stage
        if (opportunity.Stage == "Won")
        {
            opportunity.Status = "Won";
            opportunity.Probability = 100;
        }
        else if (opportunity.Stage == "Lost")
        {
            opportunity.Status = "Lost";
            opportunity.Probability = 0;
        }
        else
        {
            opportunity.Status = "Open";
        }

        opportunity.CreatedDate = DateTime.UtcNow;

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Create", "Opportunity", opportunity.OpportunityId.ToString(),
            null, $"Created opportunity {opportunity.OpportunityName} for Amount {opportunity.Amount:C}");

        return (true, null, opportunity);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity, string userId, string role)
    {
        var existing = await _context.Opportunities.FindAsync(opportunity.OpportunityId);
        if (existing == null)
        {
            return (false, "Opportunity not found.");
        }

        if (role == "SalesExecutive" && existing.AssignedTo != userId)
        {
            return (false, "Unauthorized: You can only update opportunities assigned to you.");
        }

        var validationError = ValidateOpportunityBusinessRules(opportunity);
        if (validationError != null)
        {
            return (false, validationError);
        }

        var oldSummary = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Prob: {existing.Probability}%";
        var newSummary = $"Stage: {opportunity.Stage}, Amount: {opportunity.Amount}, Prob: {opportunity.Probability}%";

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Notes = opportunity.Notes;

        if (opportunity.Stage == "Won")
        {
            existing.Status = "Won";
            existing.Probability = 100;
        }
        else if (opportunity.Stage == "Lost")
        {
            existing.Status = "Lost";
            existing.Probability = 0;
        }
        else
        {
            existing.Status = "Open";
        }

        if (role != "SalesExecutive" && !string.IsNullOrWhiteSpace(opportunity.AssignedTo))
        {
            existing.AssignedTo = opportunity.AssignedTo;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Update", "Opportunity", opportunity.OpportunityId.ToString(),
            oldSummary, newSummary);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int id, string userId, string role)
    {
        var existing = await _context.Opportunities.FindAsync(id);
        if (existing == null)
        {
            return (false, "Opportunity not found.");
        }

        if (role == "SalesExecutive")
        {
            return (false, "Unauthorized: Sales executives cannot delete opportunities.");
        }

        var summary = $"Opportunity: {existing.OpportunityName} (Amount: {existing.Amount})";
        _context.Opportunities.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Delete", "Opportunity", id.ToString(),
            summary, "Deleted opportunity record");

        return (true, null);
    }
}
