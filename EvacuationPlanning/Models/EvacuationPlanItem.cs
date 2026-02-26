using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a single vehicle-to-zone assignment in the evacuation plan.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class EvacuationPlanItem {
    [Required] public required string ZoneID { get; init; }
    [Required] public required string VehicleID { get; init; }
    [Required] public required string ETA { get; init; }
    public required int NumberOfPeople { get; init; }
}
