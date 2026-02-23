using EvacuationPlanning.Models;

namespace EvacuationPlanning.VehicleSelectors;

/// <summary>
/// Selects vehicles using a weighted score that balances capacity, fitness and proximity.
/// Vehicles whose capacity closely matches the zone's remaining people are preferred,
/// with closer vehicles winning among similar capacity fitness.
/// </summary>
/// <remarks>
/// Limitations of this strategy:
/// - Weights are arbitrary (default 0.4/0.6), edge cases may produce unintuitive results.
/// - Proximity is normalized relative to the current vehicle set, so the same vehicle
///   can score differently depending on what other vehicles are present.
/// - When all vehicles are far apart, proximity differences shrink, and it degenerates
///   into a capacity-only selector. Conversely, when all are nearby, small distance
///   differences get amplified disproportionately.
/// - Speed is not considered. A slow vehicle nearby may be preferred over a fast vehicle
///   further away, even if the fast one arrives sooner. Using ETA (distance/speed) instead
///   of raw distance would be more practical, but the requirement explicitly states to
///   "prioritize the closest vehicles" by distance.
/// </remarks>
public class WeightedScoreSelector : IVehicleSelector {
    private readonly double _distanceWeight;
    private readonly double _capacityWeight;

    public WeightedScoreSelector(double distanceWeight = 0.4, double capacityWeight = 0.6) {
        _distanceWeight = distanceWeight;
        _capacityWeight = capacityWeight;
    }

    public Vehicle Select(IEnumerable<Vehicle> vehicles, EvacuationZone zone) {
        List<Vehicle> vehicleList = vehicles.ToList();

        double maxDistance = vehicleList.Max(v =>
            GeoHelper.CalculateDistance(v.LocationCoordinates, zone.LocationCoordinates));

        if (maxDistance == 0) maxDistance = 1;

        return vehicleList
            .OrderByDescending(v => Score(v, zone, maxDistance))
            .First();
    }

    private double Score(Vehicle vehicle, EvacuationZone zone, double maxDistance) {
        double distance = GeoHelper.CalculateDistance(vehicle.LocationCoordinates, zone.LocationCoordinates);
        double normalizedProximity = 1.0 - (distance / maxDistance);

        double capacityRatio = (double)vehicle.Capacity / zone.NumberOfPeople;
        double normalizedCapacityFitness = capacityRatio >= 1.0
            ? 1.0 / capacityRatio
            : capacityRatio;

        return (_distanceWeight * normalizedProximity) + (_capacityWeight * normalizedCapacityFitness);
    }
}
