using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string userId, string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null, string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedDate = DateTime.UtcNow,
                IpAddress = ipAddress ?? "127.0.0.1"
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for user {UserId}, action {Action}", userId, action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? userId = null, string? module = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.AuditLogs.Include(a => a.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(a => a.EntityName.ToLower().Contains(module.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.ToLower() == action.ToLower());
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate <= toDate.Value.AddDays(1));
        }

        return await query.OrderByDescending(a => a.CreatedDate).Take(200).ToListAsync();
    }
}
