using EvacuationPlanning.Strategies;

namespace EvacuationPlanning.Test.Strategies;

public class GeneticStrategySanityTest : StrategySanityTestBase {
    protected override IStrategy CreateStrategy() => new GeneticStrategy();
}