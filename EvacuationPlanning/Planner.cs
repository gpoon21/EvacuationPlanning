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
    private readonly IVehicleSelector _vehicleSelector;


    public Planner(IVehicleSelector vehicleSelector) {
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
        List<Vehicle> availableVehicles = _vehicles.Values
            .Where(v => v.Capacity > 0)
            .ToList();

        List<EvacuationPlanItem> plan = [];

        foreach (Zone zone in _zones.Values
                     .OrderByDescending(z => z.Info.UrgencyLevel)
                     .ToArray()) {
            int remaining = zone.Remaining;
            if (remaining <= 0) {
                continue;
            }

            static TimeSpan GetETA(LocationCoordinates a, LocationCoordinates b, double speedKmH) {
                double distanceKm = GeoHelper.CalculateDistance(a, b);
                double travelTimeHours = distanceKm / speedKmH;
                return TimeSpan.FromHours(travelTimeHours);
            }

            while (remaining > 0 && availableVehicles.Count > 0) {
                Vehicle selectedVehicle = _vehicleSelector.Select(availableVehicles, zone.Info);
                TimeSpan eta = GetETA(selectedVehicle.LocationCoordinates, zone.Info.LocationCoordinates,
                    selectedVehicle.Speed);
                int peopleToEvacuate = Math.Min(selectedVehicle.Capacity, remaining);
                Debug.Assert(peopleToEvacuate > 0);
                plan.Add(new EvacuationPlanItem {
                    ZoneID = zone.Info.ZoneID,
                    VehicleID = selectedVehicle.VehicleID,
                    ETA = eta.Humanize(),
                    NumberOfPeople = peopleToEvacuate
                });
                availableVehicles.Remove(selectedVehicle);
                remaining -= peopleToEvacuate;
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