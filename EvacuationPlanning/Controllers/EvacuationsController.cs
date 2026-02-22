using Microsoft.AspNetCore.Mvc;

namespace EvacuationPlanning.Controllers;

[ApiController]
[Route("api/evacuations")]
public class EvacuationsController : ControllerBase {
    [HttpPost("plan")]
    public async Task<IActionResult> GeneratePlan() {
        throw new NotImplementedException();
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus() {
        throw new NotImplementedException();
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateStatus() {
        throw new NotImplementedException();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear() {
        throw new NotImplementedException();
    }
}
