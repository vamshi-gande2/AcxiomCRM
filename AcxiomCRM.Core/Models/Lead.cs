using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Web.Models;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Lead Code")]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Lead source is required.")]
    [StringLength(50)]
    public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Campaign

    [Required(ErrorMessage = "Lead status is required.")]
    [StringLength(20)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required(ErrorMessage = "Priority is required.")]
    [StringLength(20)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    [Range(0, 100000000, ErrorMessage = "Expected Value must be non-negative.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Expected Value")]
    public decimal ExpectedValue { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedToUser { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
