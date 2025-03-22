using Microsoft.EntityFrameworkCore;
using WeightMaster.Models; 

namespace WeightMaster.Config
{
    public class AppDbContext : DbContext
    {
        internal object UserBlockModels;
        public DbSet<UserBlockModel> UsersData { get; set; }
        public DbSet<UserLoginModel> UserLoginsLog { get; set; }
        public DbSet<LineMasterBlockModel> lineMasterData { get; set; }
        public DbSet<MemberBlockModel> MembersData { get; set; }
        public DbSet<TransactionLogBlockModel> transactionData { get; set; }
        public DbSet<FinalTransactionBlockModel> FinaltransactionData { get; set; }
        public DbSet<RunLog> RunLog { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = "Server=localhost;Database=weighthandlerdb;User=user;Password=password";
                optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.25"));
            }
        }

    }
}



//protected override void OnModelCreating(ModelBuilder modelBuilder)
//{
//    base.OnModelCreating(modelBuilder);

//    // Seed data for Students
//    modelBuilder.Entity<Student>().HasData(
//        new Student
//        {
//            Id = 1,
//            Name = "John Doe",
//            Age = 20,
//            Email = "john.doe@example.com"
//        },
//        new Student
//        {
//            Id = 2,
//            Name = "Jane Smith",
//            Age = 22,
//            Email = "jane.smith@example.com"
//        },
//        new Student
//        {
//            Id = 3,
//            Name = "Alice Johnson",
//            Age = 23,
//            Email = "alice.johnson@example.com"
//        }
//    );
//}