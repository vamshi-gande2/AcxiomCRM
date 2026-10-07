using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICustomerService _customerService;

    public LeadService(ApplicationDbContext context, IAuditService auditService, ICustomerService customerService)
    {
        _context = context;
        _auditService = auditService;
        _customerService = customerService;
    }

    public async Task<IEnumerable<Lead>> GetLeadsAsync(string userId, string role, string? search = null, string? status = null)
    {
        var query = _context.Leads
            .Include(l => l.AssignedToUser)
            .AsNoTracking()
            .AsQueryable();

        if (role == "SalesExecutive")
        {
            query = query.Where(l => l.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(term) ||
                l.Email.ToLower().Contains(term) ||
                l.Phone.Contains(term) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                l.LeadCode.ToLower().Contains(term));
        }

        return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
    }

    public async Task<Lead?> GetLeadByIdAsync(int id, string userId, string role)
    {
        var lead = await _context.Leads
            .Include(l => l.AssignedToUser)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .Include(l => l.Opportunities)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return null;

        if (role == "SalesExecutive" && lead.AssignedTo != userId)
        {
            return null;
        }

        return lead;
    }

    public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead, string userId)
    {
        if (string.IsNullOrWhiteSpace(lead.LeadCode))
        {
            lead.LeadCode = $"LEAD-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}";
        }

        if (string.IsNullOrWhiteSpace(lead.AssignedTo))
        {
            lead.AssignedTo = userId;
        }

        lead.CreatedDate = DateTime.UtcNow;

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Create", "Lead", lead.LeadId.ToString(),
            null, $"Created lead {lead.LeadName} ({lead.Email}) with status {lead.Status}");

        return (true, null, lead);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead, string userId, string role)
    {
        var existing = await _context.Leads.FindAsync(lead.LeadId);
        if (existing == null)
        {
            return (false, "Lead not found.");
        }

        if (role == "SalesExecutive" && existing.AssignedTo != userId)
        {
            return (false, "Unauthorized: You can only update leads assigned to you.");
        }

        // Prevent invalid transition from Converted
        if (existing.Status == "Converted" && lead.Status != "Converted")
        {
            return (false, "Invalid workflow: A converted lead cannot be transitioned back to another status.");
        }

        var oldSummary = $"Name: {existing.LeadName}, Status: {existing.Status}, Priority: {existing.Priority}";
        var newSummary = $"Name: {lead.LeadName}, Status: {lead.Status}, Priority: {lead.Priority}";

        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;

        // Managers and Admins can reassign
        if (role != "SalesExecutive" && !string.IsNullOrWhiteSpace(lead.AssignedTo))
        {
            existing.AssignedTo = lead.AssignedTo;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Update", "Lead", lead.LeadId.ToString(),
            oldSummary, newSummary);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int id, string userId, string role)
    {
        var existing = await _context.Leads.FindAsync(id);
        if (existing == null)
        {
            return (false, "Lead not found.");
        }

        if (role == "SalesExecutive")
        {
            return (false, "Unauthorized: Sales executives cannot delete leads.");
        }

        var summary = $"Lead: {existing.LeadName} ({existing.LeadCode})";
        _context.Leads.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Delete", "Lead", id.ToString(),
            summary, "Deleted lead record");

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(
        int leadId, string userId, string role, bool createOpportunity = true)
    {
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null)
        {
            return (false, "Lead not found.", null, null);
        }

        if (role == "SalesExecutive" && lead.AssignedTo != userId)
        {
            return (false, "Unauthorized: You can only convert leads assigned to you.", null, null);
        }

        if (lead.Status == "Converted")
        {
            return (false, "This lead has already been converted.", null, null);
        }

        // Check if customer with this email or phone exists
        var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c =>
            c.Email.ToLower() == lead.Email.ToLower() || c.Phone == lead.Phone);

        Customer targetCustomer;
        if (existingCustomer != null)
        {
            targetCustomer = existingCustomer;
        }
        else
        {
            targetCustomer = new Customer
            {
                CustomerCode = $"CUST-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100, 999)}",
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Address = "Converted from Lead",
                City = "N/A",
                State = "N/A",
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = lead.AssignedTo
            };
            _context.Customers.Add(targetCustomer);
            await _context.SaveChangesAsync();
        }

        Opportunity? opp = null;
        if (createOpportunity)
        {
            opp = new Opportunity
            {
                OpportunityName = $"{lead.LeadName} - Sales Deal",
                CustomerId = targetCustomer.CustomerId,
                LeadId = lead.LeadId,
                Amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m,
                Stage = "Qualification",
                Probability = 30,
                ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(30),
                Status = "Open",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo,
                Notes = $"Converted from Lead {lead.LeadCode}"
            };
            _context.Opportunities.Add(opp);
        }

        lead.Status = "Converted";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(userId, "Conversion", "Lead", lead.LeadId.ToString(),
            $"Status was {lead.Status}",
            $"Converted lead to Customer {targetCustomer.CustomerId} {(opp != null ? $"and Opportunity {opp.OpportunityId}" : "")}");

        return (true, null, targetCustomer, opp);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? leadId = null)
    {
        var normalized = email.Trim().ToLower();
        return !await _context.Leads.AnyAsync(l =>
            l.Email.ToLower() == normalized &&
            (!leadId.HasValue || l.LeadId != leadId.Value));
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? leadId = null)
    {
        var normalized = phone.Trim();
        return !await _context.Leads.AnyAsync(l =>
            l.Phone == normalized &&
            (!leadId.HasValue || l.LeadId != leadId.Value));
    }
}
