using EvacuationPlanning.Models;

namespace EvacuationPlanning.Strategies;


public interface IStrategy {
    public Dictionary<IZone, Vehicle[]> Assign(IEnumerable<Vehicle> vehicles, IEnumerable<IZone> zone);
}
