using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;

namespace TalentManagement.Data;

public static class DbInitializer
{
    public const string RecruiterRole = "Recruiter";
    public const string CandidateRole = "Candidate";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch
        {
            // If MigrateAsync encounters an issue, fallback to direct schema alteration below
        }

        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                DECLARE @sql NVARCHAR(MAX);
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Projects')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Projects' AND COLUMN_NAME = 'ProjectUrl')
                    BEGIN
                        SELECT TOP 1 @sql = 'ALTER TABLE [' + TABLE_SCHEMA + '].[Projects] ADD [ProjectUrl] NVARCHAR(1000) NULL;'
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME = 'Projects';
                        EXEC sp_executesql @sql;
                    END
                END
            ");
        }
        catch
        {
            // Ignore if column already exists or unsupported provider
        }

        if (!await roleManager.RoleExistsAsync(RecruiterRole))
        {
            await roleManager.CreateAsync(new IdentityRole(RecruiterRole));
        }
        if (!await roleManager.RoleExistsAsync(CandidateRole))
        {
            await roleManager.CreateAsync(new IdentityRole(CandidateRole));
        }

        var categories = new[]
        {
            "General Information",
            "Technical Skills",
            "Soft Skills",
            "Education & Certifications",
            "Experience"
        };

        foreach (var categoryName in categories)
        {
            if (!await context.Categories.AnyAsync(c => c.Name == categoryName))
            {
                context.Categories.Add(new Category { Name = categoryName });
            }
        }
        await context.SaveChangesAsync();

        var generalCategory = await context.Categories.FirstAsync(c => c.Name == "General Information");
        var techCategory = await context.Categories.FirstAsync(c => c.Name == "Technical Skills");
        var eduCategory = await context.Categories.FirstAsync(c => c.Name == "Education & Certifications");

        const string recruiterEmail = "recruiter@talent.local";
        var recruiterUser = await userManager.FindByEmailAsync(recruiterEmail);
        if (recruiterUser == null)
        {
            recruiterUser = new ApplicationUser
            {
                UserName = recruiterEmail,
                Email = recruiterEmail,
                EmailConfirmed = true,
                FullName = "Aminul Islam"
            };
            var result = await userManager.CreateAsync(recruiterUser, "Recruiter123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(recruiterUser, RecruiterRole);
            }
        }

        const string candidate1Email = "candidate1@talent.local";
        var candidate1 = await userManager.FindByEmailAsync(candidate1Email);
        if (candidate1 == null)
        {
            candidate1 = new ApplicationUser
            {
                UserName = candidate1Email,
                Email = candidate1Email,
                EmailConfirmed = true,
                FullName = "Rakib Hasan"
            };
            var result = await userManager.CreateAsync(candidate1, "Candidate123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(candidate1, CandidateRole);

                var profile = new CandidateProfile
                {
                    UserId = candidate1.Id,
                    FullName = candidate1.FullName,
                    Email = candidate1.Email,
                    PhoneNumber = "+1 555-0199",
                    Bio = "Passionate Junior Data Engineer and analytics enthusiast.",
                    CreatedAt = DateTime.UtcNow
                };
                context.CandidateProfiles.Add(profile);
                await context.SaveChangesAsync();
            }
        }

        const string candidate2Email = "candidate2@talent.local";
        var candidate2 = await userManager.FindByEmailAsync(candidate2Email);
        if (candidate2 == null)
        {
            candidate2 = new ApplicationUser
            {
                UserName = candidate2Email,
                Email = candidate2Email,
                EmailConfirmed = true,
                FullName = "Naem Ahmed"
            };
            var result = await userManager.CreateAsync(candidate2, "Candidate123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(candidate2, CandidateRole);

                var profile = new CandidateProfile
                {
                    UserId = candidate2.Id,
                    FullName = candidate2.FullName,
                    Email = candidate2.Email,
                    PhoneNumber = "+1 555-0244",
                    Bio = "Software developer with strong Python and SQL background.",
                    CreatedAt = DateTime.UtcNow
                };
                context.CandidateProfiles.Add(profile);
                await context.SaveChangesAsync();
            }
        }

        if (!await context.AttributeDefinitions.AnyAsync())
        {
            var englishLevel = new AttributeDefinition
            {
                Name = "English Level",
                AttributeType = AttributeType.Dropdown,
                CategoryId = generalCategory.Id,
                CreatedAt = DateTime.UtcNow,
                Options = new List<AttributeOption>
                {
                    new() { Value = "None", DisplayOrder = 1 },
                    new() { Value = "Basic", DisplayOrder = 2 },
                    new() { Value = "Intermediate", DisplayOrder = 3 },
                    new() { Value = "Fluent", DisplayOrder = 4 },
                    new() { Value = "Native", DisplayOrder = 5 }
                }
            };
            context.AttributeDefinitions.Add(englishLevel);

            var gpa = new AttributeDefinition
            {
                Name = "GPA",
                AttributeType = AttributeType.Number,
                CategoryId = eduCategory.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.AttributeDefinitions.Add(gpa);

            var python = new AttributeDefinition
            {
                Name = "Python",
                AttributeType = AttributeType.Boolean,
                CategoryId = techCategory.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.AttributeDefinitions.Add(python);

            var hadoop = new AttributeDefinition
            {
                Name = "Apache Hadoop",
                AttributeType = AttributeType.Boolean,
                CategoryId = techCategory.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.AttributeDefinitions.Add(hadoop);

            var cap = new AttributeDefinition
            {
                Name = "CAP",
                AttributeType = AttributeType.Dropdown,
                CategoryId = eduCategory.Id,
                CreatedAt = DateTime.UtcNow,
                Options = new List<AttributeOption>
                {
                    new() { Value = "None", DisplayOrder = 1 },
                    new() { Value = "Essentials", DisplayOrder = 2 },
                    new() { Value = "Pro", DisplayOrder = 3 },
                    new() { Value = "Expert", DisplayOrder = 4 }
                }
            };
            context.AttributeDefinitions.Add(cap);

            var personAge = new AttributeDefinition
            {
                Name = "Person Age",
                AttributeType = AttributeType.Number,
                CategoryId = generalCategory.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.AttributeDefinitions.Add(personAge);

            var personName = new AttributeDefinition
            {
                Name = "Person Name",
                AttributeType = AttributeType.String,
                CategoryId = generalCategory.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.AttributeDefinitions.Add(personName);

            await context.SaveChangesAsync();

            var sqlTag = new Tag { Name = "SQL" };
            var rTag = new Tag { Name = "R" };
            var pythonTag = new Tag { Name = "Python" };
            var mlTag = new Tag { Name = "Machine Learning" };
            var dataTag = new Tag { Name = "Data Analysis" };

            context.Tags.AddRange(sqlTag, rTag, pythonTag, mlTag, dataTag);
            await context.SaveChangesAsync();

            var samplePosition = new Position
            {
                Title = "Junior Data Engineer @ Acme Corp.",
                Description = "Seeking an ambitious Junior Data Engineer to design scalable pipelines and analytics workflows.",
                CreatedByRecruiterId = recruiterUser.Id,
                Status = PositionStatus.Active,
                MaxRecentProjects = 4,
                CreatedAt = DateTime.UtcNow,
                PositionAttributes = new List<PositionAttribute>
                {
                    new() { AttributeDefinitionId = englishLevel.Id, IsRequired = false, DisplayOrder = 1 },
                    new() { AttributeDefinitionId = gpa.Id, IsRequired = true, DisplayOrder = 2 },
                    new() { AttributeDefinitionId = python.Id, IsRequired = false, DisplayOrder = 3 },
                    new() { AttributeDefinitionId = hadoop.Id, IsRequired = false, DisplayOrder = 4 },
                    new() { AttributeDefinitionId = cap.Id, IsRequired = true, DisplayOrder = 5 }
                },
                TagFilters = new List<PositionTagFilter>
                {
                    new() { TagId = sqlTag.Id },
                    new() { TagId = rTag.Id },
                    new() { TagId = pythonTag.Id }
                }
            };

            context.Positions.Add(samplePosition);
            await context.SaveChangesAsync();

            var candidateProfile1 = await context.CandidateProfiles.FirstOrDefaultAsync(cp => cp.UserId == candidate1.Id);
            if (candidateProfile1 != null)
            {
                var proj1 = new Project
                {
                    CandidateProfileId = candidateProfile1.Id,
                    Title = "Sales Prediction System",
                    Description = "Machine learning pipeline analyzing seasonal retail transactions.",
                    StartDate = new DateTime(2025, 1, 15),
                    EndDate = new DateTime(2025, 6, 30),
                    CreatedAt = DateTime.UtcNow,
                    ProjectTags = new List<ProjectTag>
                    {
                        new() { TagId = pythonTag.Id },
                        new() { TagId = mlTag.Id },
                        new() { TagId = sqlTag.Id }
                    }
                };

                var proj2 = new Project
                {
                    CandidateProfileId = candidateProfile1.Id,
                    Title = "Customer Churn Analytics",
                    Description = "Statistical modeling and exploratory data analysis using R and SQL.",
                    StartDate = new DateTime(2025, 7, 1),
                    EndDate = new DateTime(2025, 11, 20),
                    CreatedAt = DateTime.UtcNow,
                    ProjectTags = new List<ProjectTag>
                    {
                        new() { TagId = rTag.Id },
                        new() { TagId = sqlTag.Id },
                        new() { TagId = dataTag.Id }
                    }
                };

                context.Projects.AddRange(proj1, proj2);
                await context.SaveChangesAsync();
            }
        }
    }
}
