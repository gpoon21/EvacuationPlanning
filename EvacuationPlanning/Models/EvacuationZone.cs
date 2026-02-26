using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable InconsistentNaming

namespace EvacuationPlanning.Models;

public interface IZone {
    LocationCoordinates LocationCoordinates { get; }
    int NumberOfPeople { get; }
    int UrgencyLevel { get; }
    string ZoneID { get; }
}

/// <summary>
/// Represents a disaster-affected area requiring evacuation.
/// </summary>
public class EvacuationZone : IZone {
    public required string ZoneID { get; init; }
    public required LocationCoordinates LocationCoordinates { get; init; }

    [Range(1, int.MaxValue)] public required int NumberOfPeople { get; init; }

    [Range(1, 5)] public required int UrgencyLevel { get; init; }
}