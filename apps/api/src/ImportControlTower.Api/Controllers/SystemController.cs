using ImportControlTower.Application.Models;
using ImportControlTower.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public class SystemController : ControllerBase
{
    private readonly ISystemService _systemService;

    public SystemController(ISystemService systemService)
    {
        _systemService = systemService;
    }

    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemInfoDto>> GetSystemInfo(CancellationToken cancellationToken)
    {
        var result = await _systemService.GetSystemInfoAsync(cancellationToken);
        return Ok(result);
    }
}
