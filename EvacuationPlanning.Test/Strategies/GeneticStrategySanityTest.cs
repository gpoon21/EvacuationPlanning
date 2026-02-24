using EvacuationPlanning.Strategies;
using EvacuationPlanning.Strategies.Genetic;

namespace EvacuationPlanning.Test.Strategies;

public class GeneticStrategySanityTest : StrategySanityTestBase {
    protected override IStrategy CreateStrategy() => new GeneticStrategy(new CoverageTimeFitnessProvider());
}