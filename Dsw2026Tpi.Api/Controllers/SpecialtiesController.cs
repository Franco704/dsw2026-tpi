using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/specialties")]
[Authorize(Policy = Policies.AdminPolicy)]
public class SpecialtiesController : AppController
{
    private readonly ISpecialitiesService _service;
    
    public SpecialtiesController(ISpecialitiesService service)
    {
        _service = service;
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize, [FromQuery] int pageIndex, [FromQuery] string? name = null)
    {
        var specialties = await _service.GetAll(pageSize, pageIndex, name);
        return Ok(specialties);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SpecialityModel.Request request)
    {
        var created = await _service.Create(request);
        return CreatedAtAction(nameof(GetAll), created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSpeciality([FromRoute] Guid id, [FromBody] SpecialityModel.Request request)
    {
        var updated = await _service.UpdateSpecialitiy(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSpeciality([FromRoute] Guid id)
    {
        await _service.DeleteSpecialitiy(id);
        return NoContent();
    }
}