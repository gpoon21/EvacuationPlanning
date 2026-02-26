using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a single vehicle-to-zone assignment in the evacuation plan.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class EvacuationPlanItem {
    public required string ZoneID { get; init; }
    public required string VehicleID { get; init; }
    public required string ETA { get; init; }
    public required int NumberOfPeople { get; init; }
}
