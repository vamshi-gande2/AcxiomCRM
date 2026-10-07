using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Tests;

public class RoleScopingTests
{
    [Fact]
    public async Task SalesExecutive_CannotDeleteCustomer_ReturnsUnauthorized()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new CustomerService(context, audit);

        var cust = new Customer
        {
            CustomerCode = "C1",
            CustomerName = "Target Client",
            Email = "client@test.com",
            Phone = "1234567890",
            CreatedBy = "sales-1"
        };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        // Act - Attempting deletion as SalesExecutive
        var (success, error) = await service.DeleteCustomerAsync(cust.CustomerId, "sales-1", "SalesExecutive");

        // Assert
        Assert.False(success);
        Assert.Contains("Unauthorized", error);
        Assert.NotNull(await context.Customers.FindAsync(cust.CustomerId));
    }

    [Fact]
    public async Task Admin_CanDeleteCustomer_Succeeds()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new CustomerService(context, audit);

        var cust = new Customer
        {
            CustomerCode = "C2",
            CustomerName = "Target Client 2",
            Email = "client2@test.com",
            Phone = "9876543211",
            CreatedBy = "sales-1"
        };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        // Act - Attempting deletion as Admin
        var (success, error) = await service.DeleteCustomerAsync(cust.CustomerId, "admin-1", "Admin");

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.Null(await context.Customers.FindAsync(cust.CustomerId));
    }
}
