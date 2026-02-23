using EvacuationPlanning.Models;

namespace EvacuationPlanning.Strategies;


public interface IStrategy {
    public Dictionary<EvacuationZone, Vehicle[]> GetPlan(IEnumerable<Vehicle> vehicles, IEnumerable<EvacuationZone> zone);
}