using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;


[Route("api/availabilities")]
[Authorize(Policy = Policies.AdminPolicy)]
public class AvailabilitiesController : AppController
{
    private readonly IAvailabilitiesService _service;
    
    public AvailabilitiesController(IAvailabilitiesService service)
    {
        _service = service;
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] AvailabilityModel.Request request)
    {
        var created = await _service.Create(request);
        return CreatedAtAction(nameof(Create), created);
    }
    
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAvailability([FromBody] AvailabilityModel.Request request)
    {
        var updated = await _service.UpdateAvailability(request);
        return Ok(updated);
    }
}