using EvacuationPlanning.Models;

namespace EvacuationPlanning.Strategies.Genetic;

/// <summary>
/// Scores a complete vehicle-to-zone assignment. Higher is better.
/// </summary>
public interface IFitnessProvider {
    public double GetFitness(IDictionary<EvacuationZone, Vehicle[]> plan);
}