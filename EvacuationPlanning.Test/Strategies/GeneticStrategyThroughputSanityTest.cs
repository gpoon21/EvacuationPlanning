using EvacuationPlanning.Strategies;
using EvacuationPlanning.Strategies.Genetic;

namespace EvacuationPlanning.Test.Strategies;

public class GeneticStrategyThroughputSanityTest : StrategySanityTestBase {
    protected override IStrategy CreateStrategy() => new GeneticStrategy(new ThroughputFitnessProvider());
}