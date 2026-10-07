using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Web.DTOs;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class LoginResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class PipelineStageReportDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class PipelineOwnerReportDto
{
    public string OwnerName { get; set; } = string.Empty;
    public int OpportunitiesCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PipelineReportDto
{
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedPipelineValue { get; set; }
    public int TotalOpenOpportunities { get; set; }
    public List<PipelineStageReportDto> Stages { get; set; } = new();
    public List<PipelineOwnerReportDto> ByOwner { get; set; } = new();
}
