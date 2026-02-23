using EvacuationPlanning.Models;

namespace EvacuationPlanning;

/// <summary>
/// Provides geographic calculations using the Haversine formula.
/// </summary>
public static class GeoHelper {
    private const double _earthRadiusKm = 6371.0;

    /// <returns>Distance in KM</returns>
    public static double CalculateDistance(LocationCoordinates from, LocationCoordinates to) {
        double lat1 = DegreesToRadians(from.Latitude);
        double lat2 = DegreesToRadians(to.Latitude);
        double deltaLat = DegreesToRadians(to.Latitude - from.Latitude);
        double deltaLon = DegreesToRadians(to.Longitude - from.Longitude);

        double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return _earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) {
        return degrees * (Math.PI / 180.0);
    }
}
