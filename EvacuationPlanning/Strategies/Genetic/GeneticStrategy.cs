using EvacuationPlanning.Models;
using GeneticSharp;

namespace EvacuationPlanning.Strategies.Genetic;

/// <summary>
/// Uses a genetic algorithm to find optimal vehicle-to-zone assignments.
/// Fitness measures evacuation throughput: people moved per unit time, weighted by zone urgency.
/// </summary>
public class GeneticStrategy : IStrategy {
    private readonly int _populationFactor;
    private readonly int _minPopulation;
    private readonly int _maxPopulation;
    private readonly int _stagnationGenerations;
    private readonly float _crossoverProbability;
    private readonly float _mutationProbability;
    private readonly IFitnessProvider _fitnessProvider;

    public GeneticStrategy(
        IFitnessProvider fitnessProvider,
        int populationFactor = 10,
        int minPopulation = 50,
        int maxPopulation = 500,
        int stagnationGenerations = 50,
        float crossoverProbability = 0.75f,
        float mutationProbability = 0.1f) {
        _populationFactor = populationFactor;
        _minPopulation = minPopulation;
        _maxPopulation = maxPopulation;
        _stagnationGenerations = stagnationGenerations;
        _crossoverProbability = crossoverProbability;
        _mutationProbability = mutationProbability;
        _fitnessProvider = fitnessProvider;
    }

    public Dictionary<IZone, Vehicle[]> Assign(IEnumerable<Vehicle> vehicles,
        IEnumerable<IZone> zones) {
        Vehicle[] vehicleArray = vehicles.ToArray();
        IZone[] zoneArray = zones.ToArray();

        if (vehicleArray.Length == 0 || zoneArray.Length == 0) {
            return new Dictionary<IZone, Vehicle[]>();
        }

        if (vehicleArray.Length == 1) {
            return AssignSingleVehicle(vehicleArray[0], zoneArray);
        }

        int scaledPopulation = _populationFactor * vehicleArray.Length * zoneArray.Length;
        int populationSize = Math.Clamp(scaledPopulation, _minPopulation, _maxPopulation);

        AssignmentChromosome adamChromosome = new(vehicleArray.Length, zoneArray.Length);
        Population population = new(populationSize, populationSize, adamChromosome);

        EvacuationFitness fitness = new(vehicleArray, zoneArray, _fitnessProvider);
        EliteSelection selection = new();
        UniformCrossover crossover = new();
        UniformMutation mutation = new(true);

        GeneticAlgorithm ga = new(population, fitness, selection, crossover, mutation) {
            Termination = new FitnessStagnationTermination(_stagnationGenerations),
            CrossoverProbability = _crossoverProbability,
            MutationProbability = _mutationProbability,
        };

        ga.Start();

        return DecodeChromosome(ga.BestChromosome, vehicleArray, zoneArray);
    }

    private Dictionary<IZone, Vehicle[]> AssignSingleVehicle(Vehicle vehicle, IZone[] zones) {
        IZone? bestZone = null;
        double bestScore = double.MinValue;

        foreach (IZone zone in zones) {
            Dictionary<IZone, Vehicle[]> candidate = new() { [zone] = [vehicle] };
            double score = _fitnessProvider.GetFitness(candidate);

            if (score > bestScore) {
                bestScore = score;
                bestZone = zone;
            }
        }

        if (bestZone == null) {
            return new Dictionary<IZone, Vehicle[]>();
        }

        return new Dictionary<IZone, Vehicle[]> { [bestZone] = [vehicle] };
    }

    internal static Dictionary<IZone, Vehicle[]> DecodeChromosome(
        IChromosome chromosome, Vehicle[] vehicleArray, IZone[] zoneArray) {
        Dictionary<IZone, List<Vehicle>> assignment = [];

        for (int i = 0; i < chromosome.Length; i++) {
            int zoneIndex = (int)chromosome.GetGene(i).Value;
            if (zoneIndex < 0) {
                continue;
            }

            IZone zone = zoneArray[zoneIndex];
            if (!assignment.TryGetValue(zone, out List<Vehicle>? list)) {
                list = [];
                assignment[zone] = list;
            }
            list.Add(vehicleArray[i]);
        }

        return assignment.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }
}

/// <summary>
/// Chromosome where each gene represents a vehicle's zone assignment.
/// Gene value is a zone index (0 to zoneCount-1) or -1 for unassigned.
/// </summary>
internal sealed class AssignmentChromosome : ChromosomeBase {
    private readonly int _zoneCount;

    public AssignmentChromosome(int vehicleCount, int zoneCount) : base(vehicleCount) {
        _zoneCount = zoneCount;
        CreateGenes();
    }

    public override Gene GenerateGene(int geneIndex) {
        // -1 means unassigned, 0..zoneCount-1 means assigned to that zone
        int value = RandomizationProvider.Current.GetInt(-1, _zoneCount);
        return new Gene(value);
    }

    public override IChromosome CreateNew() {
        return new AssignmentChromosome(Length, _zoneCount);
    }
}

/// <summary>
/// Bridges IFitnessProvider with GeneticSharp's IFitness interface.
/// Decodes a chromosome into a zone-to-vehicles mapping and delegates scoring.
/// </summary>
internal class EvacuationFitness : IFitness {
    private readonly Vehicle[] _vehicles;
    private readonly IZone[] _zones;
    private readonly IFitnessProvider _fitnessProvider;

    public EvacuationFitness(Vehicle[] vehicles, IZone[] zones, IFitnessProvider fitnessProvider) {
        _vehicles = vehicles;
        _zones = zones;
        _fitnessProvider = fitnessProvider;
    }

    public double Evaluate(IChromosome chromosome) {
        Dictionary<IZone, Vehicle[]> plan =
            GeneticStrategy.DecodeChromosome(chromosome, _vehicles, _zones);
        return _fitnessProvider.GetFitness(plan);
    }
}