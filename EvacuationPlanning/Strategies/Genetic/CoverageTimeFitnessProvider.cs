using EvacuationPlanning.Models;

namespace EvacuationPlanning.Strategies.Genetic;

/// <summary>
/// Scores assignments by coverage ratio divided by average time.
/// Per zone: (people_evacuated / people_needed) × urgency / avg_total_time_seconds.
/// Coverage is the primary driver, time efficiency is the tiebreaker.
/// Penalizes vehicles that arrive with no one left to pick up.
/// </summary>
/// <remarks>
/// This just a failed experiment I created when I tested the throughput strategy, and I saw something that
/// I thought it's not making sense. But it turns out that it makes perfect sense when I see the image of distances
/// between each point. This start is not working well, but I did not fix it since it is no longer relevant.
/// </remarks>
public class CoverageTimeFitnessProvider : IFitnessProvider {
    private readonly double _vehicleSwitchSeconds;

    public CoverageTimeFitnessProvider(double vehicleSwitchSeconds = 30.0) {
        _vehicleSwitchSeconds = vehicleSwitchSeconds;
    }

    public double GetFitness(IDictionary<EvacuationZone, Vehicle[]> plan) {
        double totalFitness = 0.0;

        foreach ((EvacuationZone zone, Vehicle[] vehicles) in plan) {
            Vehicle[] sorted = [.. vehicles.OrderBy(v =>
                GeoHelper.GetETA(v.LocationCoordinates, zone.LocationCoordinates, v.Speed).TotalSeconds)];

            int remaining = zone.NumberOfPeople;
            int totalEvacuated = 0;
            double zoneAvailableAt = 0.0;
            double totalTimeSum = 0.0;
            int activeVehicleCount = 0;

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

                totalTimeSum += totalTimeSeconds;
                activeVehicleCount++;
                totalEvacuated += peopleLoaded;
                remaining -= peopleLoaded;
            }

            if (activeVehicleCount == 0) {
                continue;
            }

            double coverage = (double)totalEvacuated / zone.NumberOfPeople;
            double avgTime = totalTimeSum / activeVehicleCount;

            totalFitness += coverage * zone.UrgencyLevel / avgTime;
        }

        return totalFitness;
    }
}