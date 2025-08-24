using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<InsurancePolicy> Policies => Set<InsurancePolicy>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ProcessedPolicyExpiration> ProcessedExpirations => Set<ProcessedPolicyExpiration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Car>()
            .HasIndex(c => c.Vin)
            .IsUnique(false); // TODO: set true and handle conflicts

        modelBuilder.Entity<InsurancePolicy>(b =>
        {
            b.Property(p => p.StartDate).IsRequired();
            b.Property(p => p.EndDate).IsRequired(); // TASK A: obligatoriu în DB
        });
        modelBuilder.Entity<Claim>(b =>
        {
            b.Property(c => c.Description).HasMaxLength(500);
            b.Property(c => c.Amount).HasColumnType("decimal(18,2)");
            b.HasOne(c => c.Car)
             .WithMany(ca => ca.Claims)
             .HasForeignKey(c => c.CarId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProcessedPolicyExpiration>(b =>
        {
            b.HasIndex(x => x.PolicyId).IsUnique();
        });
        // EndDate intentionally left nullable for a later task
    }
}

public static class SeedData
{
    public static void EnsureSeeded(AppDbContext db)
    {
        if (db.Owners.Any()) return;

        var ana = new Owner { Name = "Ana Pop", Email = "ana.pop@example.com" };
        var bogdan = new Owner { Name = "Bogdan Ionescu", Email = "bogdan.ionescu@example.com" };
        db.Owners.AddRange(ana, bogdan);
        db.SaveChanges();

        var car1 = new Car { Vin = "VIN12345", Make = "Dacia", Model = "Logan", YearOfManufacture = 2018, OwnerId = ana.Id };
        var car2 = new Car { Vin = "VIN67890", Make = "VW", Model = "Golf", YearOfManufacture = 2021, OwnerId = bogdan.Id };
        db.Cars.AddRange(car1, car2);
        db.SaveChanges();

        db.Policies.AddRange(
            new InsurancePolicy { CarId = car1.Id, Provider = "Allianz", StartDate = new DateOnly(2024,1,1), EndDate = new DateOnly(2024,12,31) },
            new InsurancePolicy { CarId = car1.Id, Provider = "Groupama", StartDate = new DateOnly(2025,1,1), EndDate = new DateOnly(2025,10,31) }, // open-ended on purpose
            new InsurancePolicy { CarId = car2.Id, Provider = "Allianz", StartDate = new DateOnly(2025,3,1), EndDate = new DateOnly(2025,9,30) },
            new InsurancePolicy { CarId = car2.Id, Provider = "Allianz", StartDate = new DateOnly(2025, 3, 1), EndDate = new DateOnly(2025, 9, 30) }
        );
     
        db.Policies.Add(new InsurancePolicy
        {
            CarId = car1.Id,
            Provider = "TestDev",
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = DateOnly.FromDateTime(DateTime.Now.Date.AddDays(-1)) // ieri
        });


        db.SaveChanges();
    }
}
