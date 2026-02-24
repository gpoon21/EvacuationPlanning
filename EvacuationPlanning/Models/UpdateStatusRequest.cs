using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Request body for updating evacuation progress at a zone.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UpdateStatusRequest {
    public required string ZoneID { get; set; }
    public required string VehicleID { get; set; }
    public required int NumberOfPeopleEvacuated { get; set; }
}