using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a transport vehicle available for evacuation operations.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Vehicle {
    public required string VehicleID { get; init; }

    [Range(1, int.MaxValue)]
    public required int Capacity { get; init; }

    public required string Type { get; init; }
    public required LocationCoordinates LocationCoordinates { get; init; }

    /// <summary>
    /// Speed in km/h
    /// </summary>
    [Range(0.1, double.MaxValue)]
    public required double Speed { get; init; }
}