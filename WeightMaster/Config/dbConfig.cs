using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using WeightMaster.Models; 

namespace WeightMaster.Config
{
    public class AppDbContext : DbContext
    {
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
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string configFilePath = Path.Combine(exeDirectory, "dbconfig.txt");

                string connectionString = "Server=localhost;Database=weighthandlerdb;User=user;Password=password"; // Default

                if (File.Exists(configFilePath))
                {
                    try
                    {
                        connectionString = File.ReadAllText(configFilePath).Trim();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading dbconfig.txt: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("dbconfig.txt not found. Using default connection string.");
                }

                optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.25"));
            }
        }
    }
}
