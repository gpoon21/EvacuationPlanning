using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a single vehicle-to-zone assignment in the evacuation plan.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class EvacuationPlanItem {
    public required string ZoneID { get; set; }
    public required string VehicleID { get; set; }
    public required string ETA { get; set; }
    public required int NumberOfPeople { get; set; }
}
