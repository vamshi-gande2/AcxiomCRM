using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AcxiomCRM.Web.Models;

namespace AcxiomCRM.Web.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = { "Admin", "Manager", "SalesExecutive" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Users
        var adminEmail = "admin@acxiom.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var managerEmail = "manager@acxiom.com";
        var managerUser = await userManager.FindByEmailAsync(managerEmail);
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                FullName = "Sales Manager",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }

        var salesEmail = "sales@acxiom.com";
        var salesUser = await userManager.FindByEmailAsync(salesEmail);
        if (salesUser == null)
        {
            salesUser = new ApplicationUser
            {
                UserName = salesEmail,
                Email = salesEmail,
                FullName = "Alex Morgan (Sales)",
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(salesUser, "Sales@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser, "SalesExecutive");
            }
        }

        // 3. Seed Customers
        if (!context.Customers.Any())
        {
            var customer1 = new Customer
            {
                CustomerCode = "CUST-1001",
                CustomerName = "Apex Global Enterprises",
                Email = "contact@apexglobal.com",
                Phone = "9876543210",
                CompanyName = "Apex Global Ltd",
                Address = "104 Tech Boulevard",
                City = "Bengaluru",
                State = "Karnataka",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                CreatedBy = salesUser!.Id
            };

            var customer2 = new Customer
            {
                CustomerCode = "CUST-1002",
                CustomerName = "Summit Healthcare Solutions",
                Email = "info@summithealth.com",
                Phone = "9812345678",
                CompanyName = "Summit Health Care",
                Address = "45 Ring Road",
                City = "Hyderabad",
                State = "Telangana",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-20),
                CreatedBy = managerUser!.Id
            };

            var customer3 = new Customer
            {
                CustomerCode = "CUST-1003",
                CustomerName = "BlueHorizon Retailers",
                Email = "procure@bluehorizon.com",
                Phone = "9988776655",
                CompanyName = "BlueHorizon Inc",
                Address = "88 Metro Lane",
                City = "Mumbai",
                State = "Maharashtra",
                Status = "Active",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                CreatedBy = salesUser!.Id
            };

            context.Customers.AddRange(customer1, customer2, customer3);
            await context.SaveChangesAsync();

            // 4. Seed Leads
            var lead1 = new Lead
            {
                LeadCode = "LEAD-2001",
                LeadName = "FinTech Labs India",
                Email = "partners@fintechlabs.in",
                Phone = "9123456780",
                CompanyName = "FinTech Labs",
                Source = "Website",
                Status = "Qualified",
                Priority = "High",
                ExpectedValue = 75000,
                CreatedDate = DateTime.UtcNow.AddDays(-15),
                AssignedTo = salesUser!.Id
            };

            var lead2 = new Lead
            {
                LeadCode = "LEAD-2002",
                LeadName = "Quantum Logistics",
                Email = "sales@quantumlog.com",
                Phone = "9823456789",
                CompanyName = "Quantum Logistics Group",
                Source = "Referral",
                Status = "Contacted",
                Priority = "Medium",
                ExpectedValue = 42000,
                CreatedDate = DateTime.UtcNow.AddDays(-8),
                AssignedTo = salesUser!.Id
            };

            var lead3 = new Lead
            {
                LeadCode = "LEAD-2003",
                LeadName = "Nova EduTech",
                Email = "admin@novaedutech.org",
                Phone = "9734567891",
                CompanyName = "Nova EduTech",
                Source = "Campaign",
                Status = "New",
                Priority = "Low",
                ExpectedValue = 20000,
                CreatedDate = DateTime.UtcNow.AddDays(-3),
                AssignedTo = managerUser!.Id
            };

            context.Leads.AddRange(lead1, lead2, lead3);
            await context.SaveChangesAsync();

            // 5. Seed Opportunities
            var opp1 = new Opportunity
            {
                OpportunityName = "Cloud ERP Migration",
                CustomerId = customer1.CustomerId,
                Amount = 120000,
                Stage = "Negotiation",
                Probability = 80,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.AddDays(-14),
                AssignedTo = salesUser!.Id,
                Notes = "Final contract review in progress."
            };

            var opp2 = new Opportunity
            {
                OpportunityName = "CRM Enterprise Upgrade",
                CustomerId = customer2.CustomerId,
                Amount = 85000,
                Stage = "Proposal",
                Probability = 60,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                Status = "Open",
                CreatedDate = DateTime.UtcNow.AddDays(-10),
                AssignedTo = managerUser!.Id,
                Notes = "Proposal shared with CTO."
            };

            var opp3 = new Opportunity
            {
                OpportunityName = "Annual Support Retainer",
                CustomerId = customer3.CustomerId,
                Amount = 45000,
                Stage = "Won",
                Probability = 100,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                Status = "Won",
                CreatedDate = DateTime.UtcNow.AddDays(-25),
                AssignedTo = salesUser!.Id,
                Notes = "Closed and agreement signed."
            };

            var opp4 = new Opportunity
            {
                OpportunityName = "Legacy DB Integration",
                CustomerId = customer1.CustomerId,
                Amount = 30000,
                Stage = "Lost",
                Probability = 0,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                Status = "Lost",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                AssignedTo = salesUser!.Id,
                Notes = "Client postponed project."
            };

            context.Opportunities.AddRange(opp1, opp2, opp3, opp4);
            await context.SaveChangesAsync();

            // 6. Seed FollowUps
            var fu1 = new FollowUp
            {
                CustomerId = customer1.CustomerId,
                Subject = "Discuss revised pricing proposal",
                FollowUpDate = DateTime.UtcNow.Date.AddDays(2),
                FollowUpType = "Call",
                Remarks = "Call procurement lead directly",
                Status = "Planned",
                AssignedTo = salesUser!.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            };

            var fu2 = new FollowUp
            {
                LeadId = lead1.LeadId,
                Subject = "Demo of AcxiomCRM features",
                FollowUpDate = DateTime.UtcNow.Date.AddDays(1),
                FollowUpType = "Meeting",
                Remarks = "Prepare standard slide deck",
                Status = "Planned",
                AssignedTo = salesUser!.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            };

            var fu3 = new FollowUp
            {
                CustomerId = customer2.CustomerId,
                Subject = "Quarterly Business Review",
                FollowUpDate = DateTime.UtcNow.Date.AddDays(-3),
                FollowUpType = "Meeting",
                Remarks = "Review completed smoothly",
                Status = "Completed",
                AssignedTo = managerUser!.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-7)
            };

            context.FollowUps.AddRange(fu1, fu2, fu3);

            // 7. Seed Activities
            var act1 = new Activity
            {
                ActivityType = "Call",
                Subject = "Introductory discovery discussion",
                Description = "Discussed CRM scaling bottlenecks",
                ActivityDate = DateTime.UtcNow.AddDays(-5),
                CustomerId = customer1.CustomerId,
                AssignedTo = salesUser!.Id,
                Status = "Completed"
            };

            var act2 = new Activity
            {
                ActivityType = "Email",
                Subject = "Sent product brochure and datasheet",
                Description = "Customer requested technical whitepaper",
                ActivityDate = DateTime.UtcNow.AddDays(-4),
                LeadId = lead1.LeadId,
                AssignedTo = salesUser!.Id,
                Status = "Completed"
            };

            context.Activities.AddRange(act1, act2);

            // 8. Seed Audit Log entries
            var audit1 = new AuditLog
            {
                UserId = adminUser!.Id,
                Action = "Login",
                EntityName = "Authentication",
                RecordId = adminUser.Id,
                OldValue = null,
                NewValue = "Admin logged in successfully",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                IpAddress = "127.0.0.1"
            };

            var audit2 = new AuditLog
            {
                UserId = salesUser!.Id,
                Action = "Create",
                EntityName = "Customer",
                RecordId = customer1.CustomerId.ToString(),
                OldValue = null,
                NewValue = "Created customer Apex Global Enterprises",
                CreatedDate = DateTime.UtcNow.AddDays(-30),
                IpAddress = "127.0.0.1"
            };

            context.AuditLogs.AddRange(audit1, audit2);
            await context.SaveChangesAsync();
        }
    }
}
