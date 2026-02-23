using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using EvacuationPlanning.Models;
using EvacuationPlanning.VehicleSelectors;
using Humanizer;

namespace EvacuationPlanning;

/// <summary>
/// Manages evacuation data in memory and syncs evacuation status.
/// </summary>
public class Planner {
    private class Zone {
        public required EvacuationZone Info { get; init; }

        public int Evacuated { get; private set; }

        public Vehicle? LastVehicle { get; private set; }

        public int Remaining {
            get { return Info.NumberOfPeople - Evacuated; }
        }

        private readonly Lock _lock = new();

        public void Evacuate(int numberOfPeople, Vehicle vehicle) {
            //guarantee that it is not possible to observe an invalid state where the evacuated number was updated 
            // but the last vehicle is not
            lock (_lock) {
                Evacuated += numberOfPeople;
                LastVehicle = vehicle;
            }
        }

        public EvacuationStatus GetStatus() {
            return new EvacuationStatus() {
                RemainingPeople = Remaining,
                TotalEvacuated = Evacuated,
                ZoneID = Info.ZoneID,
                LastVehicleUsed = JsonSerializer.Serialize(LastVehicle),
            };
        }
    }

    public enum UpdateType {
        Updated,
        Removed,
    }

    public event Action<EvacuationStatus, UpdateType>? ZoneUpdated;
    private readonly ConcurrentDictionary<string, Zone> _zones = new();
    private readonly ConcurrentDictionary<string, Vehicle> _vehicles = new();
    private readonly IStrategy _vehicleSelector;


    public Planner(IStrategy vehicleSelector) {
        _vehicleSelector = vehicleSelector;
    }


    public void SetVehicle(Vehicle vehicle) {
        _vehicles[vehicle.VehicleID] = vehicle;
    }

    public void SetZone(EvacuationZone zone) {
        Zone z = new() {
            Info = zone
        };
        _zones[zone.ZoneID] = z;
        ZoneUpdated?.Invoke(z.GetStatus(), UpdateType.Updated);
    }

    public bool RemoveVehicle(string id) {
        return _vehicles.TryRemove(id, out _);
    }

    public bool RemoveZone(string id) {
        bool removed = _zones.TryRemove(id, out Zone? zone);
        if (zone != null) ZoneUpdated?.Invoke(zone.GetStatus(), UpdateType.Removed);
        return removed;
    }

    public void Clear() {
        foreach (Zone zone in _zones.Values) {
            RemoveZone(zone.Info.ZoneID);
        }
        foreach (Vehicle vehicle in _vehicles.Values) {
            RemoveVehicle(vehicle.VehicleID);
        }
    }


    public EvacuationPlanItem[] Plan() {
        List<EvacuationPlanItem> plan = [];

        Dictionary<EvacuationZone, Vehicle[]> result =
            _vehicleSelector.GetPlan(_vehicles.Values, _zones.Values.Select(zone => zone.Info));

        foreach ((EvacuationZone zone, Vehicle[] vehicles) in result) {
            static TimeSpan GetETA(LocationCoordinates a, LocationCoordinates b, double speedKmH) {
                double distanceKm = GeoHelper.CalculateDistance(a, b);
                double travelTimeHours = distanceKm / speedKmH;
                return TimeSpan.FromHours(travelTimeHours);
            }

            int remaining = zone.NumberOfPeople;
            foreach (Vehicle vehicle in vehicles.OrderBy(v => v.Capacity)) {
                if (remaining <= 0) break;
                TimeSpan eta = GetETA(vehicle.LocationCoordinates, zone.LocationCoordinates,
                    vehicle.Speed);
                int peopleInVehicle = Math.Min(vehicle.Capacity, remaining);
                Debug.Assert(peopleInVehicle > 0);
                plan.Add(new EvacuationPlanItem {
                    ZoneID = zone.ZoneID,
                    VehicleID = vehicle.VehicleID,
                    ETA = eta.Humanize(),
                    NumberOfPeople = peopleInVehicle
                });
                remaining -= peopleInVehicle;
            }
        }

        return plan.ToArray();
    }

    public EvacuationStatus[] GetStatuses() {
        return _zones.Values.Select(z => z.GetStatus()).ToArray();
    }

    public void UpdateStatus(string zoneId, string vehicleId, int numberOfPeopleEvacuated) {
        if (!_zones.TryGetValue(zoneId, out Zone? zone)) {
            throw new KeyNotFoundException($"Zone '{zoneId}' not found.");
        }
        if (!_vehicles.TryGetValue(vehicleId, out Vehicle? vehicle)) {
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' not found");
        }
        zone.Evacuate(numberOfPeopleEvacuated, vehicle);
        ZoneUpdated?.Invoke(zone.GetStatus(), UpdateType.Updated);
    }
}