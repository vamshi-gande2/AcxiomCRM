using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Tests;

public class OpportunityValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void OpportunityAmount_MustBeGreaterThanZero_OtherwiseRejected(decimal invalidAmount)
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new OpportunityService(context, audit);

        var opp = new Opportunity
        {
            OpportunityName = "Test Opp",
            Amount = invalidAmount,
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(10),
            Stage = "Qualification",
            Status = "Open"
        };

        // Act
        var error = service.ValidateOpportunityBusinessRules(opp);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("Opportunity Amount must be greater than 0.", error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public void Probability_MustBeBetweenZeroAndHundred_OtherwiseRejected(int invalidProbability)
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new OpportunityService(context, audit);

        var opp = new Opportunity
        {
            OpportunityName = "Test Opp",
            Amount = 1000,
            Probability = invalidProbability,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(10),
            Stage = "Qualification",
            Status = "Open"
        };

        // Act
        var error = service.ValidateOpportunityBusinessRules(opp);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("Probability must be between 0 and 100.", error);
    }

    [Fact]
    public void ExpectedCloseDate_CannotBeInThePast_ForActiveOpportunity()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new OpportunityService(context, audit);

        var opp = new Opportunity
        {
            OpportunityName = "Test Opp",
            Amount = 15000,
            Probability = 50,
            ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(-1), // Yesterday
            Stage = "Proposal",
            Status = "Open"
        };

        // Act
        var error = service.ValidateOpportunityBusinessRules(opp);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("Expected Close Date cannot be in the past.", error);
    }

    [Fact]
    public void ValidOpportunity_PassesBusinessRules()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new OpportunityService(context, audit);

        var opp = new Opportunity
        {
            OpportunityName = "Valid Enterprise Deal",
            Amount = 25000,
            Probability = 75,
            ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(30),
            Stage = "Proposal",
            Status = "Open"
        };

        // Act
        var error = service.ValidateOpportunityBusinessRules(opp);

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public void WeightedAmount_CorrectlyCalculated()
    {
        // Arrange
        var opp = new Opportunity
        {
            Amount = 50000m,
            Probability = 40
        };

        // Act & Assert (50,000 * 40 / 100 = 20,000)
        Assert.Equal(20000m, opp.WeightedAmount);
    }
}
