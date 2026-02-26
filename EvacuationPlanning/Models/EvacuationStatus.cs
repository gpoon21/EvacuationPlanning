using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Tracks the current evacuation progress for a zone.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class EvacuationStatus {
    [Required] public required string ZoneID { get; init; }
    public required int TotalEvacuated { get; init; }
    public required int RemainingPeople { get; init; }
    public string? LastVehicleUsed { get; init; }
}
