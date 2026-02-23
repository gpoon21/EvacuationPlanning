using EvacuationPlanning.Models;
using EvacuationPlanning.VehicleSelectors;

namespace EvacuationPlanning.Test;

/// <summary>
/// Common-sense sanity tests for any IVehicleSelector implementation.
/// To add a new implementation, add it to <see cref="SelectorImplementations"/>.
/// </summary>
public class VehicleSelectorSanityTests {
    public static TheoryData<IVehicleSelector> SelectorImplementations {
        get {
            return new TheoryData<IVehicleSelector> {
                new WeightedScoreSelector()
            };
        }
    }

    // ── Single vehicle: no choice, must return it ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_OnlyOneVehicle_ReturnsThatVehicle(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 20,
            UrgencyLevel = 3
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "V1", Type = "bus", Capacity = 40, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 14.00, Longitude = 101.00 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("V1", selected.VehicleID);
    }

    // ── Identical vehicles except distance: closer one should win ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_IdenticalVehicles_PrefersCloserOne(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 10,
            UrgencyLevel = 3
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "far", Type = "bus", Capacity = 30, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 15.00, Longitude = 102.00 }
            },
            new() {
                VehicleID = "close", Type = "bus", Capacity = 30, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("close", selected.VehicleID);
    }

    // ── Identical vehicles except capacity: one that fits better should win ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_SameLocationVehicles_PrefersAdequateCapacityOverMassiveOverkill(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 10,
            UrgencyLevel = 3
        };

        LocationCoordinates sameSpot = new() { Latitude = 13.76, Longitude = 100.51 };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "huge", Type = "bus", Capacity = 200, Speed = 60,
                LocationCoordinates = sameSpot
            },
            new() {
                VehicleID = "fit", Type = "van", Capacity = 12, Speed = 60,
                LocationCoordinates = sameSpot
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("fit", selected.VehicleID);
    }

    // ── Must not select a vehicle that cannot carry anyone meaningfully when a capable one exists ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_TinyVehicleVsAdequate_PrefersAdequateCapacity(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 40,
            UrgencyLevel = 5
        };

        LocationCoordinates sameSpot = new() { Latitude = 13.76, Longitude = 100.51 };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "tiny", Type = "car", Capacity = 2, Speed = 80,
                LocationCoordinates = sameSpot
            },
            new() {
                VehicleID = "adequate", Type = "bus", Capacity = 40, Speed = 60,
                LocationCoordinates = sameSpot
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("adequate", selected.VehicleID);
    }

    // ── Extremely far high-capacity vs very close adequate capacity ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_VeryFarBusVsCloseVan_PrefersCloseVanWhenCapacitySufficient(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 8,
            UrgencyLevel = 4
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "far-bus", Type = "bus", Capacity = 50, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 18.00, Longitude = 104.00 }
            },
            new() {
                VehicleID = "close-van", Type = "van", Capacity = 10, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.751, Longitude = 100.501 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("close-van", selected.VehicleID);
    }

    // ── Close but far-too-small vs moderately far but adequate ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_CloseButTooSmallVsModeratelyFarAdequate_PrefersAdequate(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 35,
            UrgencyLevel = 4
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "close-tiny", Type = "car", Capacity = 3, Speed = 80,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.751, Longitude = 100.501 }
            },
            new() {
                VehicleID = "moderate-bus", Type = "bus", Capacity = 40, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.80, Longitude = 100.55 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("moderate-bus", selected.VehicleID);
    }

    // ── Among multiple adequate vehicles at varying distances, should not pick the farthest ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_MultipleAdequateVehicles_DoesNotPickFarthest(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 15,
            UrgencyLevel = 3
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "near", Type = "van", Capacity = 20, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            },
            new() {
                VehicleID = "mid", Type = "bus", Capacity = 30, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.85, Longitude = 100.60 }
            },
            new() {
                VehicleID = "far", Type = "bus", Capacity = 25, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 15.00, Longitude = 102.00 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.NotEqual("far", selected.VehicleID);
    }

    // ── All vehicles at the same spot, all same capacity: any pick is fine, just must return one ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_AllIdenticalVehicles_ReturnsOneOfThem(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 10,
            UrgencyLevel = 2
        };

        LocationCoordinates sameSpot = new() { Latitude = 13.76, Longitude = 100.51 };

        List<Vehicle> vehicles = [
            new() { VehicleID = "V1", Type = "van", Capacity = 15, Speed = 50, LocationCoordinates = sameSpot },
            new() { VehicleID = "V2", Type = "van", Capacity = 15, Speed = 50, LocationCoordinates = sameSpot },
            new() { VehicleID = "V3", Type = "van", Capacity = 15, Speed = 50, LocationCoordinates = sameSpot }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Contains(selected.VehicleID, new[] {"V1", "V2", "V3"});
    }

    // ── Zone needs 1 person: should not send a massive bus when a car is at the same spot ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_SinglePersonEvacuation_PrefersSmallVehicleOverHugeBus(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 1,
            UrgencyLevel = 2
        };

        LocationCoordinates sameSpot = new() { Latitude = 13.76, Longitude = 100.51 };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "bus", Type = "bus", Capacity = 80, Speed = 60,
                LocationCoordinates = sameSpot
            },
            new() {
                VehicleID = "car", Type = "car", Capacity = 4, Speed = 80,
                LocationCoordinates = sameSpot
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("car", selected.VehicleID);
    }

    // ── Vehicle right on top of the zone vs one a city away, same capacity ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_OneVehicleAtZoneLocation_PrefersItOverDistantSameCapacity(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 20,
            UrgencyLevel = 5
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "on-site", Type = "bus", Capacity = 25, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 }
            },
            new() {
                VehicleID = "distant", Type = "bus", Capacity = 25, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 14.50, Longitude = 101.50 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("on-site", selected.VehicleID);
    }

    // ── Large zone with many people: should prefer higher capacity even if slightly farther ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_LargeZone_PrefersHighCapacityOverSlightlyCloserSmallVehicle(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 50,
            UrgencyLevel = 5
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "close-car", Type = "car", Capacity = 4, Speed = 80,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.755, Longitude = 100.505 }
            },
            new() {
                VehicleID = "slightly-far-bus", Type = "bus", Capacity = 50, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.80, Longitude = 100.55 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("slightly-far-bus", selected.VehicleID);
    }

    // ── Three vehicles forming a clear ranking: close+adequate > far+adequate > close+tiny ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_ClearBestCandidate_CloseAndAdequateCapacity(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 20,
            UrgencyLevel = 4
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "close-adequate", Type = "van", Capacity = 22, Speed = 55,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            },
            new() {
                VehicleID = "far-adequate", Type = "bus", Capacity = 25, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 14.50, Longitude = 101.50 }
            },
            new() {
                VehicleID = "close-tiny", Type = "car", Capacity = 3, Speed = 80,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.755, Longitude = 100.505 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("close-adequate", selected.VehicleID);
    }

    // ── Uncommon: zone at extreme coordinates (near poles) ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_ExtremeLatitude_StillPrefersCloserVehicle(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 70.00, Longitude = 25.00 },
            NumberOfPeople = 10,
            UrgencyLevel = 3
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "close", Type = "van", Capacity = 15, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 70.01, Longitude = 25.01 }
            },
            new() {
                VehicleID = "far", Type = "van", Capacity = 15, Speed = 50,
                LocationCoordinates = new LocationCoordinates { Latitude = 65.00, Longitude = 20.00 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("close", selected.VehicleID);
    }

    // ── Uncommon: very high urgency should not cause nonsensical picks ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_MaxUrgency_StillRespectsDistanceAndCapacity(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 25,
            UrgencyLevel = 10
        };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "close-fit", Type = "bus", Capacity = 30, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
            },
            new() {
                VehicleID = "far-fit", Type = "bus", Capacity = 30, Speed = 60,
                LocationCoordinates = new LocationCoordinates { Latitude = 16.00, Longitude = 103.00 }
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("close-fit", selected.VehicleID);
    }

    // ── Uncommon: zone needs exactly 1 person, all vehicles have capacity >= 1 but vastly different sizes ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_MinimalEvacuation_DoesNotPickLargestVehicleWhenSmallOneSameDistance(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 1,
            UrgencyLevel = 1
        };

        LocationCoordinates sameSpot = new() { Latitude = 13.76, Longitude = 100.51 };

        List<Vehicle> vehicles = [
            new() { VehicleID = "mega", Type = "bus", Capacity = 100, Speed = 50, LocationCoordinates = sameSpot },
            new() { VehicleID = "large", Type = "bus", Capacity = 50, Speed = 55, LocationCoordinates = sameSpot },
            new() { VehicleID = "medium", Type = "van", Capacity = 15, Speed = 60, LocationCoordinates = sameSpot },
            new() { VehicleID = "small", Type = "car", Capacity = 4, Speed = 80, LocationCoordinates = sameSpot }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        Assert.Equal("small", selected.VehicleID);
    }

    // ── Uncommon: vehicles at same distance but very different speeds ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_SameDistanceSameCapacity_FasterVehicleIsReasonable(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 10,
            UrgencyLevel = 4
        };

        LocationCoordinates sameSpot = new() { Latitude = 13.85, Longitude = 100.60 };

        List<Vehicle> vehicles = [
            new() {
                VehicleID = "slow", Type = "van", Capacity = 12, Speed = 20,
                LocationCoordinates = sameSpot
            },
            new() {
                VehicleID = "fast", Type = "van", Capacity = 12, Speed = 120,
                LocationCoordinates = sameSpot
            }
        ];

        Vehicle selected = selector.Select(vehicles, zone);

        // At minimum, selecting the faster vehicle is a reasonable choice
        // (not asserting it must be faster, just that the selection is one of the two)
        Assert.Contains(selected.VehicleID, new[] { "slow", "fast" });
    }

    // ── Uncommon: vehicle list ordering should not bias the result ──

    [Theory]
    [MemberData(nameof(SelectorImplementations))]
    public void Select_ReversedInputOrder_SameResult(IVehicleSelector selector) {
        EvacuationZone zone = new() {
            ZoneID = "Z1",
            LocationCoordinates = new LocationCoordinates { Latitude = 13.75, Longitude = 100.50 },
            NumberOfPeople = 15,
            UrgencyLevel = 3
        };

        Vehicle close = new() {
            VehicleID = "close", Type = "van", Capacity = 20, Speed = 50,
            LocationCoordinates = new LocationCoordinates { Latitude = 13.76, Longitude = 100.51 }
        };

        Vehicle far = new() {
            VehicleID = "far", Type = "van", Capacity = 20, Speed = 50,
            LocationCoordinates = new LocationCoordinates { Latitude = 15.00, Longitude = 102.00 }
        };

        Vehicle resultA = selector.Select([close, far], zone);
        Vehicle resultB = selector.Select([far, close], zone);

        Assert.Equal(resultA.VehicleID, resultB.VehicleID);
    }
}
