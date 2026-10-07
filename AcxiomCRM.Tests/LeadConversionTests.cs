using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Tests;

public class LeadConversionTests
{
    [Fact]
    public async Task ConvertLead_CreatesCustomerAndOpportunity_AndMarksLeadConverted()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var customerService = new CustomerService(context, audit);
        var leadService = new LeadService(context, audit, customerService);

        var lead = new Lead
        {
            LeadCode = "LEAD-101",
            LeadName = "Innovate Corp",
            Email = "lead@innovate.com",
            Phone = "9112233445",
            CompanyName = "Innovate Solutions",
            Source = "Website",
            Status = "Qualified",
            Priority = "High",
            ExpectedValue = 50000m,
            AssignedTo = "sales-1"
        };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        // Act
        var (success, error, customer, opp) = await leadService.ConvertLeadAsync(lead.LeadId, "sales-1", "SalesExecutive", createOpportunity: true);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(customer);
        Assert.NotNull(opp);

        // Assert Lead Status
        var updatedLead = await context.Leads.FindAsync(lead.LeadId);
        Assert.Equal("Converted", updatedLead!.Status);

        // Assert Customer attributes
        Assert.Equal("Innovate Corp", customer!.CustomerName);
        Assert.Equal("lead@innovate.com", customer.Email);
        Assert.Equal("sales-1", customer.CreatedBy);

        // Assert Opportunity attributes
        Assert.Equal(50000m, opp!.Amount);
        Assert.Equal("sales-1", opp.AssignedTo);
        Assert.Equal(customer.CustomerId, opp.CustomerId);
    }

    [Fact]
    public async Task ConvertLead_WhenAlreadyConverted_FailsWithError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var customerService = new CustomerService(context, audit);
        var leadService = new LeadService(context, audit, customerService);

        var lead = new Lead
        {
            LeadCode = "LEAD-102",
            LeadName = "Alpha Corp",
            Email = "lead@alpha.com",
            Phone = "9223344556",
            Status = "Converted",
            AssignedTo = "sales-1"
        };
        context.Leads.Add(lead);
        await context.SaveChangesAsync();

        // Act
        var (success, error, _, _) = await leadService.ConvertLeadAsync(lead.LeadId, "sales-1", "SalesExecutive");

        // Assert
        Assert.False(success);
        Assert.Contains("already been converted", error);
    }
}
