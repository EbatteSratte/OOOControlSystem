using Microsoft.EntityFrameworkCore;
using OOOControlSystem.Models;
namespace OOOControlSystem
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Defect> Defects { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Defect>()
                .Property(d => d.History)
                .HasColumnType("jsonb");

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(255);
                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', NOW())");
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(255);
                entity.Property(p => p.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', NOW())");

                entity.HasOne(p => p.Creator)
                    .WithMany(u => u.CreatedProjects)
                    .HasForeignKey(p => p.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Defect>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Title).IsRequired().HasMaxLength(500);
                entity.Property(d => d.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(d => d.Priority)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);
                entity.Property(d => d.CreatedAt).HasDefaultValueSql("TIMEZONE('utc', NOW())");
                entity.Property(d => d.UpdatedAt).HasDefaultValueSql("TIMEZONE('utc', NOW())");

                entity.Property(d => d.AttachmentPaths)
                    .HasColumnType("jsonb");
                entity.Property(d => d.History)
                    .HasColumnType("jsonb");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.Defects)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Reporter)
                    .WithMany(u => u.ReportedDefects)
                    .HasForeignKey(d => d.ReportedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.AssignedUser)
                    .WithMany(u => u.AssignedDefects)
                    .HasForeignKey(d => d.AssignedToId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
