using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Services;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null, string? ipAddress = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? userId = null, string? module = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null);
}
