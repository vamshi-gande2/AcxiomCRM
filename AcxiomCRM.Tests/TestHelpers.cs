using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AcxiomCRM.Web.Data;
using AcxiomCRM.Web.Models;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Tests;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static IAuditService CreateAuditService(ApplicationDbContext context)
    {
        return new AuditService(context, new NullLogger<AuditService>());
    }
}
