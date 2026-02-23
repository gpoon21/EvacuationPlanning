using EvacuationPlanning.Models;

namespace EvacuationPlanning.VehicleSelectors;

public interface IVehicleSelector {
    public Vehicle Select(IEnumerable<Vehicle> vehicles, EvacuationZone zone);
}


