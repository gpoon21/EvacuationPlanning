using EvacuationPlanning.Models;

namespace EvacuationPlanning.VehicleSelectors;

/// <summary>
/// Selects vehicles by preferring larger capacity first, then closer distance as a tiebreaker.
/// </summary>
/// <remarks>
/// This strategy does not consider the zone's number of people or distance.
/// A large vehicle far away will always be picked over a small vehicle nearby,
/// even when the small vehicle's capacity is sufficient for the zone.
/// </remarks>
public class NaiveSelector : IVehicleSelector {
    public Vehicle Select(IEnumerable<Vehicle> vehicles, EvacuationZone zone) {
        return vehicles
            .OrderByDescending(v => v.Capacity)
            .ThenBy(v => GeoHelper.CalculateDistance(v.LocationCoordinates, zone.LocationCoordinates))
            .First();
    }
}
