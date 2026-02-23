using EvacuationPlanning.Models;
using EvacuationPlanning.VehicleSelectors;

namespace EvacuationPlanning.Test;

public class WeightedScoreSelectorTests {
    [Fact]
    public void Select_PreferNearbyVehicle_WhenFarVehicleHasExcessCapacity() {
        WeightedScoreSelector selector = new();

        // Bangkok area zone with 4 people
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 4,
            UrgencyLevel = 5
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "V1", Type = "bus", Capacity = 40, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 14.50, Longitude = 101.50 }
            },
            new() {
                VehicleID = "V2", Type = "van", Capacity = 5, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.751, Longitude = 100.501 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("V2", selected.VehicleID);
    }

    [Fact]
    public void Select_PreferLargerCapacity_WhenBothVehiclesAreEquidistant() {
        WeightedScoreSelector selector = new();

        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 30,
            UrgencyLevel = 3
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "V1", Type = "bus", Capacity = 40, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            },
            new() {
                VehicleID = "V2", Type = "van", Capacity = 10, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("V1", selected.VehicleID);
    }

    [Fact]
    public void Select_PreferBestFitCapacity_OverOversizedVehicle() {
        WeightedScoreSelector selector = new();

        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 20,
            UrgencyLevel = 4
        };

        // Both at similar distance, but bus is 4x oversized
        List<Vehicle> vehicles = [
            new() {
                VehicleID = "V1", Type = "bus", Capacity = 80, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            },
            new() {
                VehicleID = "V2", Type = "van", Capacity = 20, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("V2", selected.VehicleID);
    }

    [Fact]
    public void Select_SingleVehicle_ReturnsThatVehicle() {
        WeightedScoreSelector selector = new();

        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 10,
            UrgencyLevel = 1
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "V1", Type = "car", Capacity = 4, Speed = 80,
                LocationCoordinates = new LocationCoordinates { Latitude = 14.00, Longitude = 101.00 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("V1", selected.VehicleID);
    }

    [Fact]
    public void Select_AllVehiclesAtSameLocation_PrefersBestCapacityFit() {
        WeightedScoreSelector selector = new();

        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 15,
            UrgencyLevel = 3
        };

        LocationCoordinates sameLocation = new() { Latitude = 13.76, Longitude = 100.51 };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "V1", Type = "bus", Capacity = 50, Speed = 60,
                LocationCoordinates = sameLocation
            },
            new() {
                VehicleID = "V2", Type = "van", Capacity = 15, Speed = 50,
                LocationCoordinates = sameLocation
            },
            new() {
                VehicleID = "V3", Type = "car", Capacity = 4, Speed = 80,
                LocationCoordinates = sameLocation
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("V2", selected.VehicleID);
    }

    [Fact]
    public void Select_SlightlyCloserSmallVehicle_StillPrefersAppropriateCapacity() {
        WeightedScoreSelector selector = new();

        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 30,
            UrgencyLevel = 4
        };

        // Van is slightly closer but way too small; bus is appropriate
        List<Vehicle> vehicles = [
            new() {
                VehicleID = "V1", Type = "bus", Capacity = 30, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.77, Longitude = 100.52 }
            },
            new() {
                VehicleID = "V2", Type = "van", Capacity = 5, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.755, Longitude = 100.505 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("V1", selected.VehicleID);
    }
}
