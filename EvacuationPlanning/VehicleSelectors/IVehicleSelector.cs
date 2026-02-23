using EvacuationPlanning.Models;

namespace EvacuationPlanning.VehicleSelectors;

public interface IVehicleSelector {
    public Vehicle Select(IEnumerable<Vehicle> vehicles, EvacuationZone zone);
}


public interface IStrategy {
    public Dictionary<EvacuationZone, Vehicle[]> GetPlan(IEnumerable<Vehicle> vehicles, IEnumerable<EvacuationZone> zone);
}