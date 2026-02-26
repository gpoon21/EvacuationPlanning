using EvacuationPlanning.Models;
using EvacuationPlanning.Strategies;

namespace EvacuationPlanning.Test.Strategies;

/// <summary>
/// Abstract base for strategy sanity tests. Derive and implement CreateStrategy() to test a new implementation.
/// </summary>
public abstract class StrategySanityTestBase {
    protected abstract IStrategy CreateStrategy();

    private static LocationCoordinates Loc(double lat, double lon) => new() {
        Latitude = lat,
        Longitude = lon
    };

    private static Vehicle MakeVehicle(string id, int capacity, double speed, double lat, double lon) => new() {
        VehicleID = id,
        Capacity = capacity,
        Speed = speed,
        Type = "Test",
        LocationCoordinates = Loc(lat, lon)
    };

    private static EvacuationZone MakeZone(string id, int people, int urgency, double lat, double lon) => new() {
        ZoneID = id,
        NumberOfPeople = people,
        UrgencyLevel = urgency,
        LocationCoordinates = Loc(lat, lon)
    };

    [Fact]
    public void SingleZone_SingleVehicle_VehicleIsAssigned() {
        IStrategy strategy = CreateStrategy();
        Vehicle[] vehicles = [MakeVehicle("V1", 50, 60, 10.0, 20.0)];
        EvacuationZone[] zones = [MakeZone("Z1", 30, 5, 10.1, 20.1)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, zones);

        Assert.Contains(zones[0], result.Keys);
        Assert.Contains(vehicles[0], result[zones[0]]);
    }

    [Fact]
    public void AllZones_AppearInResult() {
        IStrategy strategy = CreateStrategy();
        Vehicle[] vehicles = [
            MakeVehicle("V1", 50, 60, 10.0, 20.0),
            MakeVehicle("V2", 30, 80, 10.5, 20.5),
            MakeVehicle("V3", 40, 70, 11.0, 21.0)
        ];
        EvacuationZone[] zones = [
            MakeZone("Z1", 20, 3, 10.1, 20.1),
            MakeZone("Z2", 25, 5, 10.6, 20.6)
        ];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, zones);

        foreach (EvacuationZone zone in zones) {
            Assert.Contains(zone, result.Keys);
        }
    }

    [Fact]
    public void NoVehicle_AssignedToMultipleZones() {
        IStrategy strategy = CreateStrategy();
        Vehicle[] vehicles = [
            MakeVehicle("V1", 50, 60, 10.0, 20.0),
            MakeVehicle("V2", 30, 80, 10.5, 20.5),
            MakeVehicle("V3", 40, 70, 11.0, 21.0)
        ];
        EvacuationZone[] zones = [
            MakeZone("Z1", 20, 3, 10.1, 20.1),
            MakeZone("Z2", 25, 5, 10.6, 20.6)
        ];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, zones);

        List<Vehicle> allAssigned = result.Values.SelectMany(v => v).ToList();
        Assert.Equal(allAssigned.Count, allAssigned.Distinct().Count());
    }

    [Fact]
    public void OnlyInputVehicles_AppearInResult() {
        IStrategy strategy = CreateStrategy();
        Vehicle[] vehicles = [
            MakeVehicle("V1", 50, 60, 10.0, 20.0),
            MakeVehicle("V2", 30, 80, 11.0, 21.0)
        ];
        EvacuationZone[] zones = [MakeZone("Z1", 20, 5, 10.1, 20.1)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, zones);

        HashSet<Vehicle> inputVehicles = [.. vehicles];
        List<Vehicle> allAssigned = result.Values.SelectMany(v => v).ToList();
        foreach (Vehicle assigned in allAssigned) {
            Assert.Contains(assigned, inputVehicles);
        }
    }

    [Fact]
    public void NearbySmallVehicle_PreferredOver_FarAwayLargeVehicle_ForSmallZone() {
        IStrategy strategy = CreateStrategy();

        // A car 1km away, capacity 5, speed 60 km/h
        Vehicle nearbyCar = MakeVehicle("Car", 5, 60, 10.0, 20.0);
        // An aircraft carrier on the other side of the world, capacity 2000, speed 50 km/h
        Vehicle farCarrier = MakeVehicle("Carrier", 2000, 50, -35.0, -150.0);

        // Zone with only 2 people, right next to the car
        EvacuationZone[] zones = [MakeZone("Z1", 2, 5, 10.01, 20.01)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign([nearbyCar, farCarrier], zones);

        Assert.Contains(zones[0], result.Keys);
        Assert.Contains(nearbyCar, result[zones[0]]);
        Assert.DoesNotContain(farCarrier, result[zones[0]]);
    }

    [Fact]
    public void LargeZone_GetsVehicleWithEnoughCapacity() {
        IStrategy strategy = CreateStrategy();

        Vehicle smallCar = MakeVehicle("SmallCar", 4, 60, 10.0, 20.0);
        Vehicle bus = MakeVehicle("Bus", 50, 50, 10.0, 20.0);
        Vehicle bigBus = MakeVehicle("BigBus", 100, 40, 10.0, 20.0);

        // Zone with 80 people - needs the big bus or multiple vehicles
        EvacuationZone[] zones = [MakeZone("Z1", 80, 5, 10.01, 20.01)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign([smallCar, bus, bigBus], zones);

        Assert.Contains(zones[0], result.Keys);
        int totalCapacity = result[zones[0]].Sum(v => v.Capacity);
        Assert.True(totalCapacity >= 80,
            $"Zone needs 80 people evacuated but assigned vehicles only have capacity {totalCapacity}");
    }

    [Fact]
    public void HighUrgencyZone_GetsAtLeastOneVehicle_WhenResourcesAreScarce() {
        IStrategy strategy = CreateStrategy();

        // Only one vehicle available
        Vehicle[] vehicles = [MakeVehicle("V1", 30, 60, 10.0, 20.0)];

        EvacuationZone lowUrgency = MakeZone("Low", 10, 1, 10.01, 20.01);
        EvacuationZone highUrgency = MakeZone("High", 10, 10, 10.02, 20.02);

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, [lowUrgency, highUrgency]);

        Assert.Contains(highUrgency, result.Keys);
        Assert.True(result[highUrgency].Length > 0,
            "High urgency zone should receive at least one vehicle when resources are scarce");
    }

    [Fact]
    public void AllVehiclesUsed_WhenTotalCapacity_LessThanTotalPeople() {
        IStrategy strategy = CreateStrategy();

        Vehicle[] vehicles = [
            MakeVehicle("V1", 10, 60, 10.0, 20.0),
            MakeVehicle("V2", 10, 60, 10.0, 20.0),
            MakeVehicle("V3", 10, 60, 10.0, 20.0)
        ];

        // 100 people but only 30 capacity total - all vehicles must be used
        EvacuationZone[] zones = [MakeZone("Z1", 100, 10, 10.01, 20.01)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, zones);

        List<Vehicle> allAssigned = result.Values.SelectMany(v => v).ToList();
        Assert.Equal(vehicles.Length, allAssigned.Count);
    }

    [Fact]
    public void MultipleZones_ClosestVehicleAssigned_WhenCapacitiesAreEqual() {
        IStrategy strategy = CreateStrategy();

        // Two vehicles with same capacity/speed but at different locations
        Vehicle vehicleNorth = MakeVehicle("VNorth", 30, 60, 12.0, 20.0);
        Vehicle vehicleSouth = MakeVehicle("VSouth", 30, 60, 8.0, 20.0);

        EvacuationZone zoneNorth = MakeZone("ZNorth", 10, 5, 12.01, 20.01);
        EvacuationZone zoneSouth = MakeZone("ZSouth", 10, 5, 7.99, 19.99);

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(
            [vehicleNorth, vehicleSouth],
            [zoneNorth, zoneSouth]);

        Assert.Contains(vehicleNorth, result[zoneNorth]);
        Assert.Contains(vehicleSouth, result[zoneSouth]);
    }

    [Fact]
    public void FastNearbyVehicle_PreferredOver_SlowFarVehicle() {
        IStrategy strategy = CreateStrategy();

        // Fast vehicle nearby
        Vehicle fast = MakeVehicle("Fast", 20, 120, 10.0, 20.0);
        // Slow vehicle far away
        Vehicle slow = MakeVehicle("Slow", 20, 10, 15.0, 25.0);

        EvacuationZone[] zones = [MakeZone("Z1", 15, 8, 10.05, 20.05)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign([fast, slow], zones);

        Assert.Contains(zones[0], result.Keys);
        Assert.Contains(fast, result[zones[0]]);
    }


    [Fact]
    public void EmptyZones_ReturnsEmptyResult() {
        IStrategy strategy = CreateStrategy();
        Vehicle[] vehicles = [MakeVehicle("V1", 50, 60, 10.0, 20.0)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, []);

        Assert.Empty(result);
    }

    [Fact]
    public void ManyVehicles_FewZones_DoesNotAssignMoreThanNeeded() {
        IStrategy strategy = CreateStrategy();

        Vehicle[] vehicles = Enumerable.Range(1, 20)
            .Select(i => MakeVehicle($"V{i}", 50, 60, 10.0 + i * 0.01, 20.0))
            .ToArray();

        // Only 5 people to evacuate - no need for 20 vehicles
        EvacuationZone[] zones = [MakeZone("Z1", 5, 5, 10.0, 20.0)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign(vehicles, zones);

        int totalAssigned = result.Values.SelectMany(v => v).Count();
        Assert.True(totalAssigned < vehicles.Length,
            $"Assigned {totalAssigned} vehicles for 5 people when {vehicles.Length} were available - wasteful");
    }

    [Fact]
    public void VehicleAtSameLocationAsZone_ShouldBePreferred() {
        IStrategy strategy = CreateStrategy();

        Vehicle colocated = MakeVehicle("Colocated", 20, 60, 10.0, 20.0);
        Vehicle distant = MakeVehicle("Distant", 20, 60, 20.0, 30.0);

        EvacuationZone[] zones = [MakeZone("Z1", 10, 5, 10.0, 20.0)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign([colocated, distant], zones);

        Assert.Contains(colocated, result[zones[0]]);
    }

    [Fact]
    public void ExtremeDistances_StrategyStillProducesValidResult() {
        IStrategy strategy = CreateStrategy();

        // North Pole vehicle
        Vehicle arctic = MakeVehicle("Arctic", 50, 60, 89.0, 0.0);
        // South Pole vehicle
        Vehicle antarctic = MakeVehicle("Antarctic", 50, 60, -89.0, 0.0);

        // Zone at equator
        EvacuationZone[] zones = [MakeZone("Equator", 30, 5, 0.0, 0.0)];

        Dictionary<IZone, Vehicle[]> result = strategy.Assign([arctic, antarctic], zones);

        Assert.Contains(zones[0], result.Keys);
        Assert.True(result[zones[0]].Length > 0, "At least one vehicle should be assigned even at extreme distances");
    }
}