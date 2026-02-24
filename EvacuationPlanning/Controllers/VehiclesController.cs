using EvacuationPlanning.Models;
using Microsoft.AspNetCore.Mvc;

namespace EvacuationPlanning.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase {
    private readonly Planner _planner;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(Planner planner, ILogger<VehiclesController> logger) {
        _planner = planner;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult AddVehicle([FromBody] Vehicle vehicle) {
        _planner.SetVehicle(vehicle);
        _logger.LogInformation("Vehicle {VehicleID} added (type={Type}, capacity={Capacity})",
            vehicle.VehicleID, vehicle.Type, vehicle.Capacity);
        return Ok(vehicle);
    }
}