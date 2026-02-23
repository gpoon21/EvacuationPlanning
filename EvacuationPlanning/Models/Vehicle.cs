using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a transport vehicle available for evacuation operations.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Vehicle {
    public required string VehicleID { get; set; }
    public required int Capacity { get; set; }
    public required string Type { get; set; }
    public required LocationCoordinates LocationCoordinates { get; set; }

    /// <summary>
    /// Speed in km/h
    /// </summary>
    public required double Speed { get; set; }
}