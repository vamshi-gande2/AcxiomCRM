using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditLogsController : Controller
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? userId = null, string? module = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        ViewBag.UserId = userId;
        ViewBag.Module = module;
        ViewBag.Action = action;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        var logs = await _auditService.GetAuditLogsAsync(userId, module, action, fromDate, toDate);
        return View(logs);
    }
}
