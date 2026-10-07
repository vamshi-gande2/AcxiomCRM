namespace AcxiomCRM.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int PendingFollowUps { get; set; }

    public string UserRole { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string FilterPeriod { get; set; } = "All";

    // Chart.js data packages (JSON strings or arrays)
    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    public List<string> OpportunityStageLabels { get; set; } = new();
    public List<decimal> OpportunityStageAmounts { get; set; } = new();

    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlySalesValues { get; set; } = new();

    public IEnumerable<Models.FollowUp> UpcomingFollowUps { get; set; } = new List<Models.FollowUp>();
    public IEnumerable<Models.Opportunity> RecentOpportunities { get; set; } = new List<Models.Opportunity>();
}
