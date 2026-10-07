using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Tests;

public class FollowUpValidationTests
{
    [Fact]
    public void FollowUpDate_EarlierThanToday_IsRejectedForNewPlannedFollowUp()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new FollowUpService(context, audit);

        var followUp = new FollowUp
        {
            Subject = "Demo Call",
            FollowUpDate = DateTime.UtcNow.Date.AddDays(-1), // Yesterday
            Status = "Planned"
        };

        // Act
        var error = service.ValidateFollowUpBusinessRules(followUp, isNew: true);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("Follow-up date cannot be earlier than today.", error);
    }

    [Fact]
    public void FollowUpDate_TodayOrFuture_PassesValidation()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new FollowUpService(context, audit);

        var followUp = new FollowUp
        {
            Subject = "Future Meeting",
            FollowUpDate = DateTime.UtcNow.Date.AddDays(2),
            Status = "Planned"
        };

        // Act
        var error = service.ValidateFollowUpBusinessRules(followUp, isNew: true);

        // Assert
        Assert.Null(error);
    }
}
