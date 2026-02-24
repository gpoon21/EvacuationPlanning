using EvacuationPlanning.Models;
using EvacuationPlanning.Strategies;
using EvacuationPlanning.Strategies.Genetic;
using Xunit.Abstractions;

namespace EvacuationPlanning.Test.Strategies;

public class GeneticStrategyManualReviewTest {
    private readonly ITestOutputHelper _output;

    public GeneticStrategyManualReviewTest(ITestOutputHelper output) {
        _output = output;
    }

    public static TheoryData<string, IFitnessProvider> FitnessProviders => new() {
        { "Throughput", new ThroughputFitnessProvider() },
        { "CoverageTime", new CoverageTimeFitnessProvider() },
    };

    private static LocationCoordinates Loc(double lat, double lon) => new() {
        Latitude = lat,
        Longitude = lon
    };

    private void PrintResult(string title, string providerName, EvacuationZone[] zones, Vehicle[] vehicles,
        Dictionary<EvacuationZone, Vehicle[]> result) {
        _output.WriteLine($"=== {title} [{providerName}] ===");
        _output.WriteLine("");

        foreach (EvacuationZone zone in zones) {
            _output.WriteLine($"Zone: {zone.ZoneID} | People: {zone.NumberOfPeople} | Urgency: {zone.UrgencyLevel}");

            if (!result.TryGetValue(zone, out Vehicle[]? assigned) || assigned.Length == 0) {
                _output.WriteLine("  (no vehicles assigned)");
                _output.WriteLine("");
                continue;
            }

            int remaining = zone.NumberOfPeople;
            foreach (Vehicle vehicle in assigned) {
                int people = Math.Min(vehicle.Capacity, remaining);
                TimeSpan eta = GeoHelper.GetETA(vehicle.LocationCoordinates, zone.LocationCoordinates, vehicle.Speed);
                double distanceKm = GeoHelper.CalculateDistance(vehicle.LocationCoordinates, zone.LocationCoordinates);

                _output.WriteLine($"  -> {vehicle.VehicleID} ({vehicle.Type}, cap={vehicle.Capacity}, speed={vehicle.Speed}km/h)");
                _output.WriteLine($"     Distance: {distanceKm:F2} km | ETA: {eta.TotalMinutes:F1} min | Carries: {people} people");

                remaining -= people;
            }

            int totalCapacity = assigned.Sum(v => v.Capacity);
            int wastedCapacity = Math.Max(0, totalCapacity - zone.NumberOfPeople);
            _output.WriteLine($"  Total assigned capacity: {totalCapacity} | Unmet: {Math.Max(0, remaining)} | Wasted capacity: {wastedCapacity}");
            _output.WriteLine("");
        }

        List<string> assignedIds = result.Values.SelectMany(v => v).Select(v => v.VehicleID).ToList();
        List<string> unassignedIds = vehicles.Where(v => !assignedIds.Contains(v.VehicleID)).Select(v => v.VehicleID).ToList();
        if (unassignedIds.Count > 0) {
            _output.WriteLine($"Unassigned vehicles: {string.Join(", ", unassignedIds)}");
        }

        _output.WriteLine($"Total vehicles: {vehicles.Length} | Assigned: {assignedIds.Count} | Unassigned: {unassignedIds.Count}");
    }

    /// <summary>
    /// Bangkok flood: 3 zones with varying urgency and population, 5 mixed vehicles.
    /// </summary>
    [Theory]
    [MemberData(nameof(FitnessProviders))]
    public void BangkokFloodScenario(string name, IFitnessProvider provider) {
        GeneticStrategy strategy = new(provider);

        EvacuationZone[] zones = [
            new() { ZoneID = "Hospital", NumberOfPeople = 200, UrgencyLevel = 5, LocationCoordinates = Loc(13.7563, 100.5018) },
            new() { ZoneID = "School", NumberOfPeople = 80, UrgencyLevel = 3, LocationCoordinates = Loc(13.7367, 100.5231) },
            new() { ZoneID = "Market", NumberOfPeople = 15, UrgencyLevel = 1, LocationCoordinates = Loc(13.7450, 100.5100) },
        ];

        Vehicle[] vehicles = [
            new() { VehicleID = "Bus-1", Capacity = 50, Speed = 40, Type = "bus", LocationCoordinates = Loc(13.7650, 100.5381) },
            new() { VehicleID = "Bus-2", Capacity = 50, Speed = 40, Type = "bus", LocationCoordinates = Loc(13.7200, 100.5300) },
            new() { VehicleID = "Van-1", Capacity = 12, Speed = 60, Type = "van", LocationCoordinates = Loc(13.7500, 100.5050) },
            new() { VehicleID = "Truck-1", Capacity = 80, Speed = 30, Type = "truck", LocationCoordinates = Loc(13.7700, 100.4900) },
            new() { VehicleID = "Boat-1", Capacity = 25, Speed = 20, Type = "boat", LocationCoordinates = Loc(13.7300, 100.5150) },
        ];

        Dictionary<EvacuationZone, Vehicle[]> result = strategy.Assign(vehicles, zones);
        PrintResult("Bangkok Flood Evacuation", name, zones, vehicles, result);
    }

    /// <summary>
    /// Aircraft carrier vs van: one massive vehicle, one small zone and one large zone.
    /// The carrier should go to the large zone, the van to the small one.
    /// </summary>
    [Theory]
    [MemberData(nameof(FitnessProviders))]
    public void AircraftCarrierVsVanScenario(string name, IFitnessProvider provider) {
        GeneticStrategy strategy = new(provider);

        EvacuationZone[] zones = [
            new() { ZoneID = "Island-Village", NumberOfPeople = 10, UrgencyLevel = 5, LocationCoordinates = Loc(12.9236, 100.8825) },
            new() { ZoneID = "Coastal-City", NumberOfPeople = 1500, UrgencyLevel = 3, LocationCoordinates = Loc(12.9300, 100.8900) },
        ];

        Vehicle[] vehicles = [
            new() { VehicleID = "Carrier", Capacity = 2000, Speed = 50, Type = "aircraft-carrier", LocationCoordinates = Loc(12.9100, 100.8700) },
            new() { VehicleID = "Van", Capacity = 15, Speed = 80, Type = "van", LocationCoordinates = Loc(12.9200, 100.8800) },
        ];

        Dictionary<EvacuationZone, Vehicle[]> result = strategy.Assign(vehicles, zones);
        PrintResult("Aircraft Carrier vs Van", name, zones, vehicles, result);
    }

    /// <summary>
    /// Single bottleneck: many vehicles but only one tiny zone with 3 people.
    /// Should assign only 1 vehicle, not waste the rest.
    /// </summary>
    [Theory]
    [MemberData(nameof(FitnessProviders))]
    public void ManyVehiclesOneTinyZone(string name, IFitnessProvider provider) {
        GeneticStrategy strategy = new(provider);

        EvacuationZone[] zones = [
            new() { ZoneID = "Rooftop", NumberOfPeople = 3, UrgencyLevel = 5, LocationCoordinates = Loc(13.7500, 100.5000) },
        ];

        Vehicle[] vehicles = Enumerable.Range(1, 10)
            .Select(i => new Vehicle {
                VehicleID = $"Bus-{i}",
                Capacity = 40,
                Speed = 50,
                Type = "bus",
                LocationCoordinates = Loc(13.7500 + i * 0.005, 100.5000 + i * 0.005),
            })
            .ToArray();

        Dictionary<EvacuationZone, Vehicle[]> result = strategy.Assign(vehicles, zones);
        PrintResult("10 Buses for 3 People on a Rooftop", name, zones, vehicles, result);
    }

    /// <summary>
    /// Large-scale: 5 zones across a region, 15 vehicles of mixed types.
    /// Tests whether the GA scales and produces reasonable assignments.
    /// </summary>
    [Theory]
    [MemberData(nameof(FitnessProviders))]
    public void LargeScaleRegionalEvacuation(string name, IFitnessProvider provider) {
        GeneticStrategy strategy = new(provider);

        EvacuationZone[] zones = [
            new() { ZoneID = "Downtown", NumberOfPeople = 500, UrgencyLevel = 5, LocationCoordinates = Loc(13.7563, 100.5018) },
            new() { ZoneID = "Suburbs-North", NumberOfPeople = 150, UrgencyLevel = 3, LocationCoordinates = Loc(13.8500, 100.5500) },
            new() { ZoneID = "Suburbs-South", NumberOfPeople = 200, UrgencyLevel = 4, LocationCoordinates = Loc(13.6500, 100.4800) },
            new() { ZoneID = "Industrial", NumberOfPeople = 80, UrgencyLevel = 2, LocationCoordinates = Loc(13.7000, 100.6000) },
            new() { ZoneID = "Airport-Area", NumberOfPeople = 300, UrgencyLevel = 4, LocationCoordinates = Loc(13.6900, 100.7500) },
        ];

        Vehicle[] vehicles = [
            new() { VehicleID = "Bus-1", Capacity = 50, Speed = 40, Type = "bus", LocationCoordinates = Loc(13.7600, 100.5100) },
            new() { VehicleID = "Bus-2", Capacity = 50, Speed = 40, Type = "bus", LocationCoordinates = Loc(13.7700, 100.5200) },
            new() { VehicleID = "Bus-3", Capacity = 50, Speed = 40, Type = "bus", LocationCoordinates = Loc(13.8400, 100.5400) },
            new() { VehicleID = "Bus-4", Capacity = 50, Speed = 40, Type = "bus", LocationCoordinates = Loc(13.6600, 100.4900) },
            new() { VehicleID = "Bus-5", Capacity = 50, Speed = 40, Type = "bus", LocationCoordinates = Loc(13.6950, 100.7400) },
            new() { VehicleID = "Truck-1", Capacity = 100, Speed = 30, Type = "truck", LocationCoordinates = Loc(13.7500, 100.5000) },
            new() { VehicleID = "Truck-2", Capacity = 100, Speed = 30, Type = "truck", LocationCoordinates = Loc(13.6800, 100.7300) },
            new() { VehicleID = "Truck-3", Capacity = 100, Speed = 30, Type = "truck", LocationCoordinates = Loc(13.6400, 100.4700) },
            new() { VehicleID = "Van-1", Capacity = 15, Speed = 70, Type = "van", LocationCoordinates = Loc(13.7550, 100.5050) },
            new() { VehicleID = "Van-2", Capacity = 15, Speed = 70, Type = "van", LocationCoordinates = Loc(13.8450, 100.5450) },
            new() { VehicleID = "Van-3", Capacity = 15, Speed = 70, Type = "van", LocationCoordinates = Loc(13.7050, 100.5950) },
            new() { VehicleID = "Van-4", Capacity = 15, Speed = 70, Type = "van", LocationCoordinates = Loc(13.6550, 100.4850) },
            new() { VehicleID = "Heli-1", Capacity = 8, Speed = 200, Type = "helicopter", LocationCoordinates = Loc(13.9000, 100.6000) },
            new() { VehicleID = "Heli-2", Capacity = 8, Speed = 200, Type = "helicopter", LocationCoordinates = Loc(13.6000, 100.4000) },
            new() { VehicleID = "Ferry-1", Capacity = 200, Speed = 25, Type = "ferry", LocationCoordinates = Loc(13.7000, 100.7600) },
        ];

        Dictionary<EvacuationZone, Vehicle[]> result = strategy.Assign(vehicles, zones);
        PrintResult("Large-Scale Regional Evacuation (5 zones, 15 vehicles)", name, zones, vehicles, result);
    }

    /// <summary>
    /// Speed matters: two vehicles at the same distance but vastly different speeds.
    /// The fast one should be preferred.
    /// </summary>
    [Theory]
    [MemberData(nameof(FitnessProviders))]
    public void SpeedDifferenceScenario(string name, IFitnessProvider provider) {
        GeneticStrategy strategy = new(provider);

        EvacuationZone[] zones = [
            new() { ZoneID = "Village", NumberOfPeople = 20, UrgencyLevel = 4, LocationCoordinates = Loc(14.0000, 100.0000) },
        ];

        Vehicle[] vehicles = [
            new() { VehicleID = "Helicopter", Capacity = 8, Speed = 250, Type = "helicopter", LocationCoordinates = Loc(14.5000, 100.5000) },
            new() { VehicleID = "Ox-Cart", Capacity = 30, Speed = 5, Type = "cart", LocationCoordinates = Loc(14.5000, 100.5000) },
        ];

        Dictionary<EvacuationZone, Vehicle[]> result = strategy.Assign(vehicles, zones);
        PrintResult("Helicopter vs Ox-Cart (same location, different speed)", name, zones, vehicles, result);
    }

    /// <summary>
    /// Urgency tiebreaker: two identical zones with different urgency, only one vehicle.
    /// The vehicle should go to the higher urgency zone.
    /// </summary>
    [Theory]
    [MemberData(nameof(FitnessProviders))]
    public void UrgencyTiebreakerScenario(string name, IFitnessProvider provider) {
        GeneticStrategy strategy = new(provider);

        EvacuationZone[] zones = [
            new() { ZoneID = "Low-Priority", NumberOfPeople = 30, UrgencyLevel = 1, LocationCoordinates = Loc(10.0000, 20.0000) },
            new() { ZoneID = "High-Priority", NumberOfPeople = 30, UrgencyLevel = 5, LocationCoordinates = Loc(10.0100, 20.0100) },
        ];

        Vehicle[] vehicles = [
            new() { VehicleID = "Bus", Capacity = 40, Speed = 50, Type = "bus", LocationCoordinates = Loc(10.0050, 20.0050) },
        ];

        Dictionary<EvacuationZone, Vehicle[]> result = strategy.Assign(vehicles, zones);
        PrintResult("Urgency Tiebreaker (1 bus, 2 equal zones, different urgency)", name, zones, vehicles, result);
    }

    /// <summary>
    /// Scattered islands: zones far apart, vehicles spread out.
    /// Tests whether proximity is respected when distances are large.
    /// </summary>
    [Theory]
    [MemberData(nameof(FitnessProviders))]
    public void ScatteredIslandsScenario(string name, IFitnessProvider provider) {
        GeneticStrategy strategy = new(provider);

        EvacuationZone[] zones = [
            new() { ZoneID = "Phuket", NumberOfPeople = 100, UrgencyLevel = 4, LocationCoordinates = Loc(7.8804, 98.3923) },
            new() { ZoneID = "Koh-Samui", NumberOfPeople = 60, UrgencyLevel = 3, LocationCoordinates = Loc(9.5120, 100.0134) },
            new() { ZoneID = "Koh-Lipe", NumberOfPeople = 25, UrgencyLevel = 5, LocationCoordinates = Loc(6.5000, 99.3000) },
        ];

        Vehicle[] vehicles = [
            new() { VehicleID = "Ferry-Phuket", Capacity = 150, Speed = 30, Type = "ferry", LocationCoordinates = Loc(7.8500, 98.4000) },
            new() { VehicleID = "Ferry-Samui", Capacity = 120, Speed = 30, Type = "ferry", LocationCoordinates = Loc(9.4800, 100.0400) },
            new() { VehicleID = "Speedboat-1", Capacity = 20, Speed = 80, Type = "speedboat", LocationCoordinates = Loc(6.5500, 99.2500) },
            new() { VehicleID = "Speedboat-2", Capacity = 20, Speed = 80, Type = "speedboat", LocationCoordinates = Loc(7.0000, 99.0000) },
        ];

        Dictionary<EvacuationZone, Vehicle[]> result = strategy.Assign(vehicles, zones);
        PrintResult("Scattered Islands (Phuket, Samui, Lipe)", name, zones, vehicles, result);
    }
}