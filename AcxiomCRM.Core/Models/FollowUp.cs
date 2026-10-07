using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Web.Models;

public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Follow-up date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Follow-Up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.Date;

    [Required(ErrorMessage = "Follow-up type is required.")]
    [StringLength(50)]
    [Display(Name = "Type")]
    public string FollowUpType { get; set; } = "Call"; // Call, Email, Meeting

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [Required(ErrorMessage = "Assigned user is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedToUser { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
