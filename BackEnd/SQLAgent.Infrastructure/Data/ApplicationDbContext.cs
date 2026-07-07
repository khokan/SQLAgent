using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using SQLAgent.Core.Models;

namespace SQLAgent.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Turnover> Turnovers { get; set; }
    public DbSet<ChatHistory> ChatHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Company configuration
        modelBuilder.Entity<Company>()
            .HasKey(c => c.Id);
        
        modelBuilder.Entity<Company>()
            .HasIndex(c => c.Code)
            .IsUnique();

        // Turnover configuration
        modelBuilder.Entity<Turnover>()
            .HasKey(t => t.Id);
        
        modelBuilder.Entity<Turnover>()
            .HasOne(t => t.Company)
            .WithMany(c => c.Turnovers)
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Turnover>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        // ChatHistory configuration
        modelBuilder.Entity<ChatHistory>()
            .HasKey(ch => ch.Id);
        
        modelBuilder.Entity<ChatHistory>()
            .HasOne(ch => ch.User)
            .WithMany(u => u.ChatHistories)
            .HasForeignKey(ch => ch.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed sample data
        SeedSampleData(modelBuilder);
    }

    private void SeedSampleData(ModelBuilder modelBuilder)
    {
        // Sample companies
        modelBuilder.Entity<Company>().HasData(
            new Company { Id = 1, Name = "TechCorp", Code = "TECH01", Description = "Technology Solutions", IsActive = true },
            new Company { Id = 2, Name = "FinanceHub", Code = "FIN01", Description = "Financial Services", IsActive = true },
            new Company { Id = 3, Name = "RetailPlus", Code = "RET01", Description = "Retail Operations", IsActive = true }
        );

        // Sample turnovers
        modelBuilder.Entity<Turnover>().HasData(
            // TechCorp 2024
            new Turnover { Id = 1, CompanyId = 1, Amount = 1500000, Year = 2024, Quarter = 1, Currency = "USD", Description = "Q1 2024" },
            new Turnover { Id = 2, CompanyId = 1, Amount = 1600000, Year = 2024, Quarter = 2, Currency = "USD", Description = "Q2 2024" },
            new Turnover { Id = 3, CompanyId = 1, Amount = 1750000, Year = 2024, Quarter = 3, Currency = "USD", Description = "Q3 2024" },
            new Turnover { Id = 4, CompanyId = 1, Amount = 1900000, Year = 2024, Quarter = 4, Currency = "USD", Description = "Q4 2024" },
            
            // TechCorp 2023
            new Turnover { Id = 5, CompanyId = 1, Amount = 1200000, Year = 2023, Quarter = 1, Currency = "USD", Description = "Q1 2023" },
            new Turnover { Id = 6, CompanyId = 1, Amount = 1300000, Year = 2023, Quarter = 2, Currency = "USD", Description = "Q2 2023" },
            new Turnover { Id = 7, CompanyId = 1, Amount = 1400000, Year = 2023, Quarter = 3, Currency = "USD", Description = "Q3 2023" },
            new Turnover { Id = 8, CompanyId = 1, Amount = 1450000, Year = 2023, Quarter = 4, Currency = "USD", Description = "Q4 2023" },
            
            // FinanceHub 2024
            new Turnover { Id = 9, CompanyId = 2, Amount = 2000000, Year = 2024, Quarter = 1, Currency = "USD", Description = "Q1 2024" },
            new Turnover { Id = 10, CompanyId = 2, Amount = 2100000, Year = 2024, Quarter = 2, Currency = "USD", Description = "Q2 2024" },
            new Turnover { Id = 11, CompanyId = 2, Amount = 2250000, Year = 2024, Quarter = 3, Currency = "USD", Description = "Q3 2024" },
            new Turnover { Id = 12, CompanyId = 2, Amount = 2400000, Year = 2024, Quarter = 4, Currency = "USD", Description = "Q4 2024" },
            
            // RetailPlus 2024
            new Turnover { Id = 13, CompanyId = 3, Amount = 3000000, Year = 2024, Quarter = 1, Currency = "USD", Description = "Q1 2024" },
            new Turnover { Id = 14, CompanyId = 3, Amount = 3200000, Year = 2024, Quarter = 2, Currency = "USD", Description = "Q2 2024" },
            new Turnover { Id = 15, CompanyId = 3, Amount = 3500000, Year = 2024, Quarter = 3, Currency = "USD", Description = "Q3 2024" },
            new Turnover { Id = 16, CompanyId = 3, Amount = 3800000, Year = 2024, Quarter = 4, Currency = "USD", Description = "Q4 2024" }
        );

        // Sample user with properly hashed password (Admin@123)
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        
        modelBuilder.Entity<User>().HasData(
            new User 
            { 
                Id = 1, 
                Username = "admin", 
                Email = "admin@cnsweb.com", 
                FullName = "Administrator",
                PasswordHash = hashedPassword,  // Properly hashed with BCrypt
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
