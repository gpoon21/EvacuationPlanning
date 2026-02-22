using Microsoft.AspNetCore.Mvc;

namespace EvacuationPlanning.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> AddVehicle() {
        throw new NotImplementedException();
    }
}
