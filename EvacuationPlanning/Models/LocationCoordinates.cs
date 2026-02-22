namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a geographic location with latitude and longitude.
/// </summary>
public class LocationCoordinates {
    public required double Latitude { get; set; }
    public required double Longitude { get; set; }
}
