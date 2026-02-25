using System.ComponentModel.DataAnnotations;

namespace EvacuationPlanning.Models;

/// <summary>
/// Represents a geographic location with latitude and longitude.
/// </summary>
public class LocationCoordinates {
    [Range(-90, 90)]
    public required double Latitude { get; set; }

    [Range(-180, 180)]
    public required double Longitude { get; set; }
}
