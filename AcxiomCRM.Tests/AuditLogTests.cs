using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Tests;

public class AuditLogTests
{
    [Fact]
    public async Task AuditLog_RecordsActionUserAndMetadata_Correctly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(Guid.NewGuid().ToString());
        var audit = TestDbContextFactory.CreateAuditService(context);

        // Act
        await audit.LogAsync(
            userId: "user-admin",
            action: "Create",
            entityName: "Customer",
            recordId: "123",
            oldValue: null,
            newValue: "Created customer Acme Ltd",
            ipAddress: "192.168.1.1"
        );

        // Assert
        var logs = await audit.GetAuditLogsAsync();
        Assert.Single(logs);
        var log = logs.First();
        Assert.Equal("user-admin", log.UserId);
        Assert.Equal("Create", log.Action);
        Assert.Equal("Customer", log.EntityName);
        Assert.Equal("123", log.RecordId);
        Assert.Equal("Created customer Acme Ltd", log.NewValue);
        Assert.Equal("192.168.1.1", log.IpAddress);
    }
}
