using System.Reflection.Emit;
using Document.Repository.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Document.Repository.Data;
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
    {
    }
    public DbSet<College> Colleges { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Programme> Programmes { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Models.Entities.Document> Documents { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TagCategory> TagCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project - Tag Many-to-Many relationship
        builder.Entity<Project>()
            .HasMany(e => e.Tags)
            .WithMany();

        // Student - User One-to-One relationship
        builder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne()
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add indexes for performance and security
        builder.Entity<Student>()
            .HasIndex(s => s.UserId)
            .IsUnique();

        builder.Entity<Student>()
            .HasIndex(s => s.RegistrationNumber);

        builder.Entity<Project>()
            .HasIndex(p => p.Status);

        builder.Entity<Project>()
            .HasIndex(p => p.CreatedDate);

        builder.Entity<Project>()
            .HasIndex(p => p.StudentId);

        builder.Entity<College>()
            .HasIndex(c => c.Name);

        builder.Entity<Department>()
            .HasIndex(d => d.CollegeId);

        builder.Entity<Programme>()
            .HasIndex(p => p.DepartmentId);

        builder.Entity<Tag>()
            .HasIndex(t => t.TagCategoryId);

        // Configure cascade delete behaviors
        builder.Entity<Project>()
            .HasOne(p => p.Student)
            .WithMany(s => s.Projects)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Models.Entities.Document>()
            .HasOne(d => d.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Department>()
            .HasOne(d => d.College)
            .WithMany(c => c.Departments)
            .HasForeignKey(d => d.CollegeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Programme>()
            .HasOne(p => p.Department)
            .WithMany(d => d.Programmes)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    public DbSet<Document.Repository.Models.Entities.Notice> Notice { get; set; } 
    public DbSet<Document.Repository.Models.Entities.SliderImage> SliderImage { get; set; } = default!;

}

public class ApplicationUser : IdentityUser
{
}
