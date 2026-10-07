using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.ViewModels;

public class ReportsViewModel
{
    public string ActiveReport { get; set; } = "pipeline";
    public string? DateRange { get; set; }

    public IEnumerable<Customer>? Customers { get; set; }
    public IEnumerable<Lead>? Leads { get; set; }
    public IEnumerable<Opportunity>? Opportunities { get; set; }
    public IEnumerable<FollowUp>? FollowUps { get; set; }
    public IEnumerable<Activity>? Activities { get; set; }
    public IEnumerable<AuditLog>? AuditLogs { get; set; }

    public int TotalLeadsCount { get; set; }
    public int ConvertedLeadsCount { get; set; }
    public decimal LeadConversionRate => TotalLeadsCount > 0 ? Math.Round((decimal)ConvertedLeadsCount / TotalLeadsCount * 100m, 1) : 0;

    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
}
