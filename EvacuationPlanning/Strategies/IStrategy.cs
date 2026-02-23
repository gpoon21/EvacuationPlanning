using EvacuationPlanning.Models;

namespace EvacuationPlanning.Strategies;


public interface IStrategy {
    public Dictionary<EvacuationZone, Vehicle[]> Assign(IEnumerable<Vehicle> vehicles, IEnumerable<EvacuationZone> zone);
}