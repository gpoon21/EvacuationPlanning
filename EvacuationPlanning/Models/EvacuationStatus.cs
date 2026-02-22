using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Tracks the current evacuation progress for a zone.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class EvacuationStatus {
    public required string ZoneID { get; set; }
    public required int TotalEvacuated { get; set; }
    public required int RemainingPeople { get; set; }
    public string? LastVehicleUsed { get; set; }
}
