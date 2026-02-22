using Microsoft.AspNetCore.Mvc;

namespace EvacuationPlanning.Controllers;

[ApiController]
[Route("api/evacuation-zones")]
public class EvacuationZonesController : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> AddZone() {
        throw new NotImplementedException();
    }
}
