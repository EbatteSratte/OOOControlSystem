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
    }
}
