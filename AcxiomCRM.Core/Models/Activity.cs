using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Web.Models;

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity type is required.")]
    [StringLength(50)]
    [Display(Name = "Activity Type")]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Assigned user is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedToUser { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Completed"; // Planned, Completed, Cancelled

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
