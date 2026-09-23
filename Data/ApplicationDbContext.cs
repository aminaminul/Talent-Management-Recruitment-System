using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Models.Entities;

namespace TalentManagement.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PositionTagFilter> PositionTagFilters => Set<PositionTagFilter>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    public DbSet<CV> CVs => Set<CV>();
    public DbSet<CVAttributeValue> CVAttributeValues => Set<CVAttributeValue>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            b.HasOne(u => u.CandidateProfile)
                .WithOne(p => p.User)
                .HasForeignKey<CandidateProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CandidateProfile>(b =>
        {
            b.HasIndex(cp => cp.UserId).IsUnique();
            b.HasIndex(cp => cp.Email);
            b.Property(cp => cp.FullName).HasMaxLength(150).IsRequired();
            b.Property(cp => cp.Email).HasMaxLength(256).IsRequired();
            b.Property(cp => cp.PhoneNumber).HasMaxLength(30);
            b.Property(cp => cp.ProfileImageUrl).HasMaxLength(1000);
            b.Property(cp => cp.ImagePublicId).HasMaxLength(200);
            b.Property(cp => cp.RowVersion).IsRowVersion();

            b.HasMany(cp => cp.Projects)
                .WithOne(p => p.CandidateProfile)
                .HasForeignKey(p => p.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(cp => cp.CVs)
                .WithOne(c => c.CandidateProfile)
                .HasForeignKey(c => c.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(b =>
        {
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(c => c.Name).IsUnique();

            b.HasMany(c => c.Attributes)
                .WithOne(a => a.Category)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttributeDefinition>(b =>
        {
            b.Property(a => a.Name).HasMaxLength(100).IsRequired();
            b.Property(a => a.RowVersion).IsRowVersion();

            b.HasMany(a => a.Options)
                .WithOne(o => o.AttributeDefinition)
                .HasForeignKey(o => o.AttributeDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(a => a.PositionAttributes)
                .WithOne(pa => pa.AttributeDefinition)
                .HasForeignKey(pa => pa.AttributeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(a => a.CVAttributeValues)
                .WithOne(cav => cav.AttributeDefinition)
                .HasForeignKey(cav => cav.AttributeDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttributeOption>(b =>
        {
            b.Property(o => o.Value).HasMaxLength(150).IsRequired();
        });

        builder.Entity<Position>(b =>
        {
            b.Property(p => p.Title).HasMaxLength(200).IsRequired();
            b.Property(p => p.Description).IsRequired();
            b.Property(p => p.RowVersion).IsRowVersion();

            b.HasOne(p => p.CreatedByRecruiter)
                .WithMany(u => u.CreatedPositions)
                .HasForeignKey(p => p.CreatedByRecruiterId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(p => p.PositionAttributes)
                .WithOne(pa => pa.Position)
                .HasForeignKey(pa => pa.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.TagFilters)
                .WithOne(ptf => ptf.Position)
                .HasForeignKey(ptf => ptf.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(p => p.CVs)
                .WithOne(c => c.Position)
                .HasForeignKey(c => c.PositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PositionAttribute>(b =>
        {
            b.HasIndex(pa => new { pa.PositionId, pa.AttributeDefinitionId }).IsUnique();
        });

        builder.Entity<Tag>(b =>
        {
            b.Property(t => t.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(t => t.Name).IsUnique();
        });

        builder.Entity<PositionTagFilter>(b =>
        {
            b.HasIndex(ptf => new { ptf.PositionId, ptf.TagId }).IsUnique();

            b.HasOne(ptf => ptf.Tag)
                .WithMany(t => t.PositionTagFilters)
                .HasForeignKey(ptf => ptf.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Project>(b =>
        {
            b.Property(p => p.Title).HasMaxLength(200).IsRequired();
            b.Property(p => p.Description).IsRequired();
            b.Property(p => p.RowVersion).IsRowVersion();

            b.HasMany(p => p.ProjectTags)
                .WithOne(pt => pt.Project)
                .HasForeignKey(pt => pt.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProjectTag>(b =>
        {
            b.HasIndex(pt => new { pt.ProjectId, pt.TagId }).IsUnique();

            b.HasOne(pt => pt.Tag)
                .WithMany(t => t.ProjectTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CV>(b =>
        {
            b.HasIndex(c => new { c.CandidateProfileId, c.PositionId }).IsUnique();
            b.Property(c => c.RowVersion).IsRowVersion();

            b.HasMany(c => c.AttributeValues)
                .WithOne(cav => cav.CV)
                .HasForeignKey(cav => cav.CVId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CVAttributeValue>(b =>
        {
            b.HasIndex(cav => new { cav.CVId, cav.AttributeDefinitionId }).IsUnique();
            b.Property(cav => cav.RowVersion).IsRowVersion();
        });
    }
}
