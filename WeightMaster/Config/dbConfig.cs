using Microsoft.EntityFrameworkCore;
using WeightMaster.Models; 

namespace WeightMaster.Config
{
    public class AppDbContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<UserBlockModel> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = "Server=localhost;Database=weighthandlerdb;User=root;Password=";
                optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.25"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed data for Students
            modelBuilder.Entity<Student>().HasData(
                new Student
                {
                    Id = 1,
                    Name = "John Doe",
                    Age = 20,
                    Email = "john.doe@example.com"
                },
                new Student
                {
                    Id = 2,
                    Name = "Jane Smith",
                    Age = 22,
                    Email = "jane.smith@example.com"
                },
                new Student
                {
                    Id = 3,
                    Name = "Alice Johnson",
                    Age = 23,
                    Email = "alice.johnson@example.com"
                }
            );
        }
    }
}
