using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService
{
    private readonly AppDbContext _db;

    public CarService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars
            .Include(c => c.Owner)
            .Select(c => new CarDto(
                c.Id,
                c.Vin,
                c.Make,
                c.Model,
                c.YearOfManufacture,
                c.OwnerId,
                c.Owner.Name,
                c.Owner.Email
            ))
            .ToListAsync();
    }


    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }

    public async Task<ClaimDto> RegisterClaimAsync(long carId, DateOnly claimDate, string? description, decimal amount)
    {
        var car = await _db.Cars.FindAsync(carId);
        if (car is null) throw new KeyNotFoundException($"Car {carId} not found");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0");

        var claim = new Claim
        {
            CarId = carId,
            ClaimDate = claimDate,
            Description = description,
            Amount = amount
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();

        return new ClaimDto(
            claim.Id,
            claim.CarId,
            claim.ClaimDate.ToString("yyyy-MM-dd"),
            claim.Description,
            claim.Amount
        );
    }


    public async Task<List<HistoryItemDto>> GetHistoryAsync(long carId)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        var policies = await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new { p.StartDate, p.EndDate, p.Provider })
            .ToListAsync();

        var claims = await _db.Claims
            .Where(c => c.CarId == carId)
            .Select(c => new { c.ClaimDate, c.Description, c.Amount })
            .ToListAsync();

        var items = new List<HistoryItemDto>();

        foreach (var p in policies)
        {
            items.Add(new HistoryItemDto("policy_start",
                p.StartDate.ToString("yyyy-MM-dd"),
                p.Provider,
                $"Policy started ({p.Provider})"));

            items.Add(new HistoryItemDto("policy_end",
                p.EndDate.ToString("yyyy-MM-dd"),
                p.Provider,
                $"Policy ended ({p.Provider})"));
        }

        foreach (var c in claims)
        {
            items.Add(new HistoryItemDto("claim",
                c.ClaimDate.ToString("yyyy-MM-dd"),
                null,
                $"{c.Description} | {c.Amount:0.00}"));
        }

        return items.OrderBy(i => i.Date).ToList();
    }
}
