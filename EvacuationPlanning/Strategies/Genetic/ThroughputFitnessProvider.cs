using EvacuationPlanning.Models;

namespace EvacuationPlanning.Strategies.Genetic;

/// <summary>
/// Scores assignments by total throughput: sum of (people_loaded / total_time) × urgency per vehicle.
/// Penalizes vehicles that arrive with no one left to pick up.
/// </summary>
public class ThroughputFitnessProvider : IFitnessProvider {
    private readonly double _vehicleSwitchSeconds;

    public ThroughputFitnessProvider(double vehicleSwitchSeconds = 30.0) {
        _vehicleSwitchSeconds = vehicleSwitchSeconds;
    }

    public double GetFitness(IDictionary<IZone, Vehicle[]> plan) {
        double totalFitness = 0.0;

        foreach ((IZone zone, Vehicle[] vehicles) in plan) {
            Vehicle[] sorted = [.. vehicles.OrderBy(v =>
                GeoHelper.GetETA(v.LocationCoordinates, zone.LocationCoordinates, v.Speed).TotalSeconds)];

            int remaining = zone.NumberOfPeople;
            double zoneAvailableAt = 0.0;

            foreach (Vehicle vehicle in sorted) {
                double arrivalTime = GeoHelper
                    .GetETA(vehicle.LocationCoordinates, zone.LocationCoordinates, vehicle.Speed).TotalSeconds;

                if (remaining <= 0) {
                    totalFitness -= arrivalTime;
                    continue;
                }

                int peopleLoaded = Math.Min(vehicle.Capacity, remaining);
                double loadingTimeSeconds = peopleLoaded;

                double effectiveStart = Math.Max(arrivalTime, zoneAvailableAt);
                double waitTime = effectiveStart - arrivalTime;
                double totalTimeSeconds = arrivalTime + waitTime + loadingTimeSeconds;

                zoneAvailableAt = effectiveStart + loadingTimeSeconds + _vehicleSwitchSeconds;

                double throughput = peopleLoaded / totalTimeSeconds;
                totalFitness += throughput * zone.UrgencyLevel;

                remaining -= peopleLoaded;
            }
        }

        return totalFitness;
    }
}