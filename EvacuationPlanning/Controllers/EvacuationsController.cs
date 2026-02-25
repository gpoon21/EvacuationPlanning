using EvacuationPlanning.Models;
using Microsoft.AspNetCore.Mvc;

namespace EvacuationPlanning.Controllers;

[ApiController]
[Route("api/evacuations")]
public class EvacuationsController : ControllerBase {
    private readonly Planner _planner;
    private readonly ILogger<EvacuationsController> _logger;

    public EvacuationsController(Planner planner, ILogger<EvacuationsController> logger) {
        _planner = planner;
        _logger = logger;
    }

    [HttpPost("plan")]
    public IActionResult GeneratePlan() {
        EvacuationPlanItem[] plan = _planner.Plan();
        foreach (EvacuationPlanItem item in plan) {
            _logger.LogInformation("Assignment: vehicle {VehicleID} -> zone {ZoneID}, ETA={ETA}, people={NumberOfPeople}",
                item.VehicleID, item.ZoneID, item.ETA, item.NumberOfPeople);
        }
        _logger.LogInformation("Evacuation plan generated with {Count} assignments", plan.Length);
        return Ok(plan);
    }

    [HttpGet("status")]
    public IActionResult GetStatus() {
        EvacuationStatus[] statuses = _planner.GetStatuses();
        return Ok(statuses);
    }

    [HttpPut("update")]
    public IActionResult UpdateStatus([FromBody] UpdateStatusRequest request) {
        try {
            _planner.UpdateStatus(request.ZoneID, request.VehicleID, request.NumberOfPeopleEvacuated);
        }
        catch (KeyNotFoundException ex) {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex) {
            return BadRequest(ex.Message);
        }
        _logger.LogInformation("Zone {ZoneID} updated: {Count} people evacuated via vehicle {VehicleID}",
            request.ZoneID, request.NumberOfPeopleEvacuated, request.VehicleID);
        return Ok();
    }

    [HttpDelete("clear")]
    public IActionResult Clear() {
        _planner.Clear();
        _logger.LogInformation("All evacuation data cleared");
        return Ok();
    }
}