using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Web.Models;

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security

    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string? IpAddress { get; set; }
}
