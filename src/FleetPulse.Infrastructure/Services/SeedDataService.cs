using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using FleetPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FleetPulse.Infrastructure.Services;

public class SeedDataService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public SeedDataService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetPulseDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        await SeedUsersAsync(db, cancellationToken);
        await SeedExceptionRulesAsync(db, cancellationToken);
        await SeedDemoVehicleAsync(db, cancellationToken);

        if (bool.TryParse(_configuration["SeedData:DemoData"], out var demoData) ? demoData : true)
            await SeedDemoDataAsync(db, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedUsersAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            // Backfill rename so an existing "Ops Dispatcher" becomes plain "Dispatcher".
            var dispatcherUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "dispatcher@fleetpulse.com", ct);
            if (dispatcherUser is { FullName: "Ops Dispatcher" })
            {
                dispatcherUser.FullName = "Dispatcher";
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        var admin = new User
        {
            Email = "admin@fleetpulse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123!"),
            FullName = "System Administrator",
            Role = UserRole.Admin
        };

        var dispatcher = new User
        {
            Email = "dispatcher@fleetpulse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dispatcher@123!"),
            FullName = "Dispatcher",
            Role = UserRole.Dispatcher
        };

        var driver = new User
        {
            Email = "driver@fleetpulse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123!"),
            FullName = "Truck Driver",
            Role = UserRole.Driver,
            Driver = new Driver { LicenseNumber = "DL-2024-001" }
        };

        db.Users.AddRange(admin, dispatcher, driver);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedExceptionRulesAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        if (await db.ExceptionRules.AnyAsync(ct))
            return;

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin, ct);
        var createdBy = admin?.Id ?? Guid.Empty;

        var rules = new List<ExceptionRule>
        {
            new()
            {
                RuleType = ExceptionRuleType.StopTooLong,
                ThresholdMinutes = 10,
                Severity = ExceptionSeverity.Warning,
                IsEnabled = true,
                CreatedBy = createdBy
            },
            new()
            {
                RuleType = ExceptionRuleType.DeliveryTrendingLate,
                ThresholdPercent = 15,
                Severity = ExceptionSeverity.Warning,
                IsEnabled = true,
                CreatedBy = createdBy
            },
            new()
            {
                RuleType = ExceptionRuleType.RouteDeviation,
                ThresholdValue = 500,
                Severity = ExceptionSeverity.Critical,
                IsEnabled = true,
                CreatedBy = createdBy
            },
            new()
            {
                RuleType = ExceptionRuleType.SpeedAnomaly,
                ThresholdValue = 120,
                Severity = ExceptionSeverity.Critical,
                IsEnabled = true,
                CreatedBy = createdBy
            }
        };

        db.ExceptionRules.AddRange(rules);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDemoVehicleAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        if (!await db.Vehicles.AnyAsync(ct))
        {
            db.Vehicles.Add(new Vehicle
            {
                Name = "Delivery Van",
                LicensePlate = "AA-1001",
                Type = VehicleType.Van,
                IsActive = true
            });
            await db.SaveChangesAsync(ct);
        }

        var driverUser = await db.Users.Include(u => u.Driver).FirstOrDefaultAsync(u => u.Email == "driver@fleetpulse.com", ct);
        if (driverUser?.Driver is { VehicleId: null })
        {
            var vehicle = await db.Vehicles.FirstOrDefaultAsync(ct);
            if (vehicle is not null)
            {
                driverUser.Driver.VehicleId = vehicle.Id;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task SeedDemoDataAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        await SeedDemoDriverAsync(db, ct,
            email: "marina@fleetpulse.com",
            fullName: "Marina Reyes",
            license: "DL-2024-002",
            vehicle: new Vehicle { Name = "Box Truck", LicensePlate = "AB-2002", Type = VehicleType.Truck, IsActive = true });

        await SeedDemoDriverAsync(db, ct,
            email: "omar@fleetpulse.com",
            fullName: "Omar Haddad",
            license: "DL-2024-003",
            vehicle: new Vehicle { Name = "Courier Bike", LicensePlate = "AC-3003", Type = VehicleType.Motorcycle, IsActive = true });

        await SeedExtraFleetAsync(db, ct);
        await SeedUnassignedDriverAsync(db, ct);
        await SeedDemoTripAsync(db, ct);
        await SeedSecondDemoTripAsync(db, ct);
    }

    private static async Task SeedExtraFleetAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        // Idempotent additions so the dashboard always has a few vehicles to show,
        // including at least one inactive so the "show inactive" view has data.
        await AddVehicleIfMissingAsync(db, ct, new Vehicle
        {
            Name = "Refrigerated Truck",
            LicensePlate = "AD-4004",
            Type = VehicleType.Truck,
            IsActive = true
        });

        await AddVehicleIfMissingAsync(db, ct, new Vehicle
        {
            Name = "Cargo Scooter",
            LicensePlate = "AE-5005",
            Type = VehicleType.Motorcycle,
            IsActive = false
        });
    }

    private static async Task AddVehicleIfMissingAsync(FleetPulseDbContext db, CancellationToken ct, Vehicle vehicle)
    {
        if (await db.Vehicles.AnyAsync(v => v.LicensePlate == vehicle.LicensePlate, ct))
            return;

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedUnassignedDriverAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        // A demo driver with no vehicle yet — used to demonstrate assigning a vehicle.
        if (await db.Users.AnyAsync(u => u.Email == "priya@fleetpulse.com", ct))
            return;

        db.Users.Add(new User
        {
            Email = "priya@fleetpulse.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123!"),
            FullName = "Priya Shah",
            Role = UserRole.Driver,
            Driver = new Driver { LicenseNumber = "DL-2024-004" }
        });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDemoDriverAsync(FleetPulseDbContext db, CancellationToken ct, string email, string fullName, string license, Vehicle vehicle)
    {
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return;

        var existing = await db.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == vehicle.LicensePlate, ct);
        Vehicle dbVehicle = existing ?? AddVehicle(db, vehicle);

        db.Users.Add(new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123!"),
            FullName = fullName,
            Role = UserRole.Driver,
            Driver = new Driver { LicenseNumber = license, VehicleId = dbVehicle.Id }
        });

        await db.SaveChangesAsync(ct);
    }

    private static Vehicle AddVehicle(FleetPulseDbContext db, Vehicle vehicle)
    {
        db.Vehicles.Add(vehicle);
        return vehicle;
    }

    private static async Task SeedDemoTripAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        var driverUser = await db.Users.Include(u => u.Driver).FirstOrDefaultAsync(u => u.Email == "driver@fleetpulse.com", ct);
        var driver = driverUser?.Driver;
        if (driver is null || driver.VehicleId is null)
            return;

        if (await db.Trips.AnyAsync(t => t.DriverId == driver.Id && t.Status != TripStatus.Completed, ct))
            return;

        var now = DateTime.UtcNow;
        var trip = new Trip
        {
            DriverId = driver.Id,
            VehicleId = driver.VehicleId.Value,
            Status = TripStatus.InProgress,
            Origin = "Lower Manhattan Hub",
            Destination = "Midtown West Terminal",
            OriginLat = 40.7128m,
            OriginLng = -74.0060m,
            DestLat = 40.7590m,
            DestLng = -73.9845m,
            EstimatedDeparture = now.AddMinutes(-35),
            ActualDeparture = now.AddMinutes(-35),
            EstimatedArrival = now.AddMinutes(25),
            CreatedAt = now.AddMinutes(-35)
        };

        foreach (var (minutesAgo, lat, lng, speed) in DemoMovingPoints)
            trip.Locations.Add(new LocationHistory
            {
                DriverId = driver.Id,
                Latitude = lat,
                Longitude = lng,
                SpeedKmh = speed,
                RecordedAt = now.AddMinutes(-minutesAgo)
            });

        for (var minutesAgo = 23; minutesAgo >= 0; minutesAgo--)
        {
            var jitter = (minutesAgo % 3) * 0.00003m;
            trip.Locations.Add(new LocationHistory
            {
                DriverId = driver.Id,
                Latitude = DemoParkLat + jitter,
                Longitude = DemoParkLng + (minutesAgo % 2 == 0 ? jitter : -jitter),
                SpeedKmh = 0,
                RecordedAt = now.AddMinutes(-minutesAgo)
            });
        }

        db.Trips.Add(trip);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSecondDemoTripAsync(FleetPulseDbContext db, CancellationToken ct)
    {
        // A second active delivery makes the dispatcher view useful immediately: it shows
        // a separate driver, live position, and a clearly visible origin-to-destination route.
        await SeedDemoDriverAsync(db, ct,
            email: "aarav@fleetpulse.com",
            fullName: "Aarav Mehta",
            license: "DL-2024-005",
            vehicle: new Vehicle { Name = "City Cargo Van", LicensePlate = "AF-6006", Type = VehicleType.Van, IsActive = true });

        var driverUser = await db.Users.Include(u => u.Driver)
            .FirstOrDefaultAsync(u => u.Email == "aarav@fleetpulse.com", ct);
        var driver = driverUser?.Driver;
        if (driver?.VehicleId is null)
            return;

        if (await db.Trips.AnyAsync(t => t.DriverId == driver.Id && t.Status != TripStatus.Completed, ct))
            return;

        var now = DateTime.UtcNow;
        var trip = new Trip
        {
            DriverId = driver.Id,
            VehicleId = driver.VehicleId.Value,
            Status = TripStatus.InProgress,
            Origin = "Chelsea Fulfillment Center",
            Destination = "Williamsburg Drop Hub",
            OriginLat = 40.7465m,
            OriginLng = -74.0010m,
            DestLat = 40.7180m,
            DestLng = -73.9580m,
            EstimatedDeparture = now.AddMinutes(-20),
            ActualDeparture = now.AddMinutes(-20),
            EstimatedArrival = now.AddMinutes(30),
            CreatedAt = now.AddMinutes(-20),
            Locations =
            {
                new() { DriverId = driver.Id, Latitude = 40.7465m, Longitude = -74.0010m, SpeedKmh = 0m, RecordedAt = now.AddMinutes(-20) },
                new() { DriverId = driver.Id, Latitude = 40.7390m, Longitude = -73.9890m, SpeedKmh = 24m, RecordedAt = now.AddMinutes(-12) },
                new() { DriverId = driver.Id, Latitude = 40.7320m, Longitude = -73.9770m, SpeedKmh = 27m, RecordedAt = now.AddMinutes(-5) },
                new() { DriverId = driver.Id, Latitude = 40.7260m, Longitude = -73.9670m, SpeedKmh = 22m, RecordedAt = now.AddMinutes(-1) }
            }
        };

        db.Trips.Add(trip);
        await db.SaveChangesAsync(ct);
    }

    private static readonly (int MinutesAgo, decimal Lat, decimal Lng, decimal Speed)[] DemoMovingPoints =
    {
        (35, 40.71280m, -74.00600m, 0m),
        (33, 40.71290m, -74.00570m, 22m),
        (31, 40.71320m, -74.00520m, 26m),
        (29, 40.71350m, -74.00480m, 24m),
        (27, 40.71380m, -74.00430m, 23m),
        (25, 40.71400m, -74.00380m, 18m),
        (24, 40.71410m, -74.00345m, 5m)
    };

    private const decimal DemoParkLat = 40.71412m;
    private const decimal DemoParkLng = -74.00342m;
}
