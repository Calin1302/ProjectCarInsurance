// InsuranceValidityTests.cs — MSTest
using System.Linq;
using System.Threading.Tasks;
using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CarInsurance.Api.Tests
{
    [TestClass]
    public class InsuranceValidityTests
    {
        // Creează un DbContext EF InMemory + seed minim pentru teste
        private static AppDbContext BuildDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var db = new AppDbContext(options);

            // seed: 1 owner, 1 car, 1 poliță 2024-01-01..2024-12-31
            var owner = new Owner { Name = "Test Owner", Email = "owner@test.com" };
            db.Owners.Add(owner); db.SaveChanges();

            var car = new Car { Vin = "TESTVIN", Make = "Make", Model = "Model", YearOfManufacture = 2020, OwnerId = owner.Id };
            db.Cars.Add(car); db.SaveChanges();

            db.Policies.Add(new InsurancePolicy
            {
                CarId = car.Id,
                Provider = "TestIns",
                StartDate = new DateOnly(2024, 1, 1),
                EndDate = new DateOnly(2024, 12, 31)  // capetele incluse
            });
            db.SaveChanges();

            return db;
        }

        [TestMethod]
        public async Task StartDate_Inclusive_ReturnsTrue()
        {
            using var db = BuildDb();
            var service = new CarService(db);
            var carId = await db.Cars.Select(c => c.Id).FirstAsync();

            var ok = await service.IsInsuranceValidAsync(carId, new DateOnly(2024, 1, 1));

            Assert.IsTrue(ok);
        }

        [TestMethod]
        public async Task EndDate_Inclusive_ReturnsTrue()
        {
            using var db = BuildDb();
            var service = new CarService(db);
            var carId = await db.Cars.Select(c => c.Id).FirstAsync();

            var ok = await service.IsInsuranceValidAsync(carId, new DateOnly(2024, 12, 31));

            Assert.IsTrue(ok);
        }

        [TestMethod]
        public async Task DayBeforeStart_ReturnsFalse()
        {
            using var db = BuildDb();
            var service = new CarService(db);
            var carId = await db.Cars.Select(c => c.Id).FirstAsync();

            var ok = await service.IsInsuranceValidAsync(carId, new DateOnly(2023, 12, 31));

            Assert.IsFalse(ok);
        }

        [TestMethod]
        public async Task DayAfterEnd_ReturnsFalse()
        {
            using var db = BuildDb();
            var service = new CarService(db);
            var carId = await db.Cars.Select(c => c.Id).FirstAsync();

            var ok = await service.IsInsuranceValidAsync(carId, new DateOnly(2025, 1, 1));

            Assert.IsFalse(ok);
        }

        [TestMethod]
        public async Task NonExistentCar_ThrowsKeyNotFound()
        {
            using var db = BuildDb();
            var service = new CarService(db);

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
                await service.IsInsuranceValidAsync(9999, new DateOnly(2024, 6, 1)));
        }
    }
}
