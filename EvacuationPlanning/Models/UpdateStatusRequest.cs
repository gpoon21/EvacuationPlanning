using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace EvacuationPlanning.Models;

/// <summary>
/// Request body for updating evacuation progress at a zone.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UpdateStatusRequest {
    public required string ZoneID { get; set; }
    public required string VehicleID { get; set; }

    [Range(1, int.MaxValue)]
    public required int NumberOfPeopleEvacuated { get; set; }
}