using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a disaster-affected area requiring evacuation.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class EvacuationZone {
    public required string ZoneID { get; init; }
    public required LocationCoordinates LocationCoordinates { get; init; }

    [Range(1, int.MaxValue)]
    public required int NumberOfPeople { get; init; }

    [Range(1, 5)]
    public required int UrgencyLevel { get; init; }
}
