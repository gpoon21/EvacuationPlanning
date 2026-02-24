using EvacuationPlanning.Models;
using Microsoft.AspNetCore.Mvc;

namespace EvacuationPlanning.Controllers;

[ApiController]
[Route("api/evacuation-zones")]
public class EvacuationZonesController : ControllerBase {
    private readonly Planner _planner;
    private readonly ILogger<EvacuationZonesController> _logger;

    public EvacuationZonesController(Planner planner, ILogger<EvacuationZonesController> logger) {
        _planner = planner;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult AddZone([FromBody] EvacuationZone zone) {
        _planner.SetZone(zone);
        _logger.LogInformation("Zone {ZoneID} added (people={NumberOfPeople}, urgency={UrgencyLevel})",
            zone.ZoneID, zone.NumberOfPeople, zone.UrgencyLevel);
        return Ok(zone);
    }
}