using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateCustomer_WithDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new CustomerService(context, audit);

        var existing = new Customer
        {
            CustomerCode = "CUST-001",
            CustomerName = "Existing Customer",
            Email = "duplicate@test.com",
            Phone = "9876543210",
            CreatedBy = "user-1",
            Status = "Active"
        };
        context.Customers.Add(existing);
        await context.SaveChangesAsync();

        var duplicate = new Customer
        {
            CustomerCode = "CUST-002",
            CustomerName = "Another Customer",
            Email = "DUPLICATE@TEST.COM", // Case-insensitive test
            Phone = "9123456789",
            CreatedBy = "user-1",
            Status = "Active"
        };

        // Act
        var (success, error, created) = await service.CreateCustomerAsync(duplicate, "user-1");

        // Assert
        Assert.False(success);
        Assert.Contains("email address already exists", error);
        Assert.Null(created);
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicatePhone_ReturnsFailure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new CustomerService(context, audit);

        var existing = new Customer
        {
            CustomerCode = "CUST-001",
            CustomerName = "Existing Customer",
            Email = "unique1@test.com",
            Phone = "9988776655",
            CreatedBy = "user-1",
            Status = "Active"
        };
        context.Customers.Add(existing);
        await context.SaveChangesAsync();

        var duplicate = new Customer
        {
            CustomerCode = "CUST-002",
            CustomerName = "Another Customer",
            Email = "unique2@test.com",
            Phone = "9988776655",
            CreatedBy = "user-1",
            Status = "Active"
        };

        // Act
        var (success, error, created) = await service.CreateCustomerAsync(duplicate, "user-1");

        // Assert
        Assert.False(success);
        Assert.Contains("phone number already exists", error);
        Assert.Null(created);
    }

    [Fact]
    public async Task GetCustomers_SalesExecutiveRole_OnlyReturnsAssignedCustomers()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new CustomerService(context, audit);

        context.Customers.AddRange(
            new Customer { CustomerCode = "C1", CustomerName = "Sales 1 Client", Email = "c1@test.com", Phone = "1111111111", CreatedBy = "sales-1" },
            new Customer { CustomerCode = "C2", CustomerName = "Sales 2 Client", Email = "c2@test.com", Phone = "2222222222", CreatedBy = "sales-2" }
        );
        await context.SaveChangesAsync();

        // Act - Requesting as sales-1 with SalesExecutive role
        var results = await service.GetCustomersAsync("sales-1", "SalesExecutive");

        // Assert
        Assert.Single(results);
        Assert.Equal("C1", results.First().CustomerCode);
    }

    [Fact]
    public async Task GetCustomers_AdminRole_ReturnsAllCustomers()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);
        var service = new CustomerService(context, audit);

        context.Customers.AddRange(
            new Customer { CustomerCode = "C1", CustomerName = "Client 1", Email = "c1@test.com", Phone = "1111111111", CreatedBy = "sales-1" },
            new Customer { CustomerCode = "C2", CustomerName = "Client 2", Email = "c2@test.com", Phone = "2222222222", CreatedBy = "sales-2" }
        );
        await context.SaveChangesAsync();

        // Act - Requesting as admin with Admin role
        var results = await service.GetCustomersAsync("admin-user", "Admin");

        // Assert
        Assert.Equal(2, results.Count());
    }
}
