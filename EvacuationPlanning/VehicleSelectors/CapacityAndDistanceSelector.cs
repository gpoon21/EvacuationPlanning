using EvacuationPlanning.Models;

namespace EvacuationPlanning.VehicleSelectors;

/// <summary>
/// Selects vehicles by preferring larger capacity first, then closer distance as tiebreaker.
/// This favors fewer trips by picking the biggest vehicle that can serve the zone.
/// </summary>
public class CapacityAndDistanceSelector : IVehicleSelector {
    public Vehicle Select(IEnumerable<Vehicle> vehicles, EvacuationZone zone) {
        return vehicles
            .OrderByDescending(v => v.Capacity)
            .ThenBy(v => GeoHelper.CalculateDistance(v.LocationCoordinates, zone.LocationCoordinates))
            .First();
    }
}
