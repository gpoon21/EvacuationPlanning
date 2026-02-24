using EvacuationPlanning.Models;
using GeneticSharp;

namespace EvacuationPlanning.Strategies;

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
    private readonly double _vehicleSwitchSeconds;

    public GeneticStrategy(
        int populationFactor = 10,
        int minPopulation = 50,
        int maxPopulation = 500,
        int stagnationGenerations = 50,
        float crossoverProbability = 0.75f,
        float mutationProbability = 0.1f,
        double vehicleSwitchSeconds = 30.0) {
        _populationFactor = populationFactor;
        _minPopulation = minPopulation;
        _maxPopulation = maxPopulation;
        _stagnationGenerations = stagnationGenerations;
        _crossoverProbability = crossoverProbability;
        _mutationProbability = mutationProbability;
        _vehicleSwitchSeconds = vehicleSwitchSeconds;
    }

    public Dictionary<EvacuationZone, Vehicle[]> Assign(IEnumerable<Vehicle> vehicles,
        IEnumerable<EvacuationZone> zones) {
        Vehicle[] vehicleArray = vehicles.ToArray();
        EvacuationZone[] zoneArray = zones.ToArray();

        if (vehicleArray.Length == 0 || zoneArray.Length == 0) {
            return new Dictionary<EvacuationZone, Vehicle[]>();
        }

        if (vehicleArray.Length == 1) {
            return AssignSingleVehicle(vehicleArray[0], zoneArray);
        }

        int scaledPopulation = _populationFactor * vehicleArray.Length * zoneArray.Length;
        int populationSize = Math.Clamp(scaledPopulation, _minPopulation, _maxPopulation);

        AssignmentChromosome adamChromosome = new(vehicleArray.Length, zoneArray.Length);
        Population population = new(populationSize, populationSize, adamChromosome);

        EvacuationFitness fitness = new(vehicleArray, zoneArray, _vehicleSwitchSeconds);
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

    private static Dictionary<EvacuationZone, Vehicle[]> AssignSingleVehicle(Vehicle vehicle, EvacuationZone[] zones) {
        EvacuationZone? bestZone = null;
        double bestScore = -1.0;

        foreach (EvacuationZone zone in zones) {
            int peopleLoaded = Math.Min(vehicle.Capacity, zone.NumberOfPeople);
            double travelTimeSeconds = GeoHelper
                .GetETA(vehicle.LocationCoordinates, zone.LocationCoordinates, vehicle.Speed).TotalSeconds;
            double loadingTimeSeconds = peopleLoaded;
            double score = peopleLoaded / (travelTimeSeconds + loadingTimeSeconds) * zone.UrgencyLevel;

            if (score > bestScore) {
                bestScore = score;
                bestZone = zone;
            }
        }

        if (bestZone == null) {
            return new Dictionary<EvacuationZone, Vehicle[]>();
        }

        return new Dictionary<EvacuationZone, Vehicle[]> { [bestZone] = [vehicle] };
    }

    private static Dictionary<EvacuationZone, Vehicle[]> DecodeChromosome(
        IChromosome chromosome, Vehicle[] vehicleArray, EvacuationZone[] zoneArray) {
        Dictionary<EvacuationZone, List<Vehicle>> assignment = [];

        for (int i = 0; i < chromosome.Length; i++) {
            int zoneIndex = (int)chromosome.GetGene(i).Value;
            if (zoneIndex < 0) {
                continue;
            }

            EvacuationZone zone = zoneArray[zoneIndex];
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
/// Evaluates a complete vehicle-to-zone assignment.
/// Score = sum over all zones of (per-vehicle throughput) × urgency.
/// Per-vehicle throughput = people_loaded / (travel_time + loading_time + switch_time).
/// Loading time assumes 1 person/sec per vehicle. A switch penalty is applied for each
/// vehicle after the first, making fewer larger vehicles preferable to many smaller ones.
/// </summary>
internal class EvacuationFitness : IFitness {
    private readonly Vehicle[] _vehicles;
    private readonly EvacuationZone[] _zones;
    private readonly double _vehicleSwitchSeconds;

    public EvacuationFitness(Vehicle[] vehicles, EvacuationZone[] zones, double vehicleSwitchSeconds) {
        _vehicles = vehicles;
        _zones = zones;
        _vehicleSwitchSeconds = vehicleSwitchSeconds;
    }

    public double Evaluate(IChromosome chromosome) {
        // Group vehicles by zone index
        Dictionary<int, List<int>> zoneToVehicleIndices = [];
        for (int i = 0; i < chromosome.Length; i++) {
            int zoneIndex = (int)chromosome.GetGene(i).Value;
            if (zoneIndex < 0) {
                continue;
            }

            if (!zoneToVehicleIndices.TryGetValue(zoneIndex, out List<int>? list)) {
                list = [];
                zoneToVehicleIndices[zoneIndex] = list;
            }
            list.Add(i);
        }

        double totalFitness = 0.0;

        for (int z = 0; z < _zones.Length; z++) {
            if (!zoneToVehicleIndices.TryGetValue(z, out List<int>? vehicleIndices)) {
                continue;
            }

            EvacuationZone zone = _zones[z];

            // Sort vehicles by ETA so the fastest-arriving vehicles load first
            vehicleIndices.Sort((a, b) => {
                double etaA = GeoHelper.GetETA(_vehicles[a].LocationCoordinates, zone.LocationCoordinates, _vehicles[a].Speed).TotalSeconds;
                double etaB = GeoHelper.GetETA(_vehicles[b].LocationCoordinates, zone.LocationCoordinates, _vehicles[b].Speed).TotalSeconds;
                return etaA.CompareTo(etaB);
            });

            int remaining = zone.NumberOfPeople;
            // Tracks when the zone is ready for the next vehicle (after loading + switch)
            double zoneAvailableAt = 0.0;

            foreach (int vi in vehicleIndices) {
                Vehicle vehicle = _vehicles[vi];
                double arrivalTime = GeoHelper
                    .GetETA(vehicle.LocationCoordinates, zone.LocationCoordinates, vehicle.Speed).TotalSeconds;

                // Vehicle arrived but no one left to pick up — penalize the wasted travel time
                if (remaining <= 0) {
                    totalFitness -= arrivalTime;
                    continue;
                }

                int peopleLoaded = Math.Min(vehicle.Capacity, remaining);
                double loadingTimeSeconds = peopleLoaded;

                // Vehicle must wait if it arrives before the zone is available
                double effectiveStart = Math.Max(arrivalTime, zoneAvailableAt);
                double waitTime = effectiveStart - arrivalTime;
                double totalTimeSeconds = arrivalTime + waitTime + loadingTimeSeconds;

                zoneAvailableAt = effectiveStart + loadingTimeSeconds + _vehicleSwitchSeconds;

                double throughput = peopleLoaded / totalTimeSeconds;
                totalFitness += throughput * zone.UrgencyLevel;

                remaining -= peopleLoaded;
            }
        }

        return totalFitness;
    }
}