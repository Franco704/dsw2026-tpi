using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/specialties")]
[Authorize]
public class SpecialtiesController : AppController
{
    private readonly ISpecialitiesService _service;

    public SpecialtiesController(ISpecialitiesService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize, [FromQuery] int pageIndex, [FromQuery] string? name = null, [FromQuery] bool? active = null)
    {
        var isActive = User.IsInRole(Roles.Administrator) ? active : true;

        var specialties = await _service.GetAllSpecialtiesAsync(pageSize, pageIndex, name, isActive);
        return Ok(specialties);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SpecialtyModel.Request request)
    {
        var created = await _service.CreateSpecialtyAsync(request);
        return CreatedAtAction(nameof(GetAll), created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSpeciality([FromRoute] Guid id, [FromBody] SpecialtyModel.Request request)
    {
        var updated = await _service.UpdateSpecialtyAsync(id, request);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(
        typeof(string),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [Authorize(Policy = Policies.AdminPolicy)]
    public async Task<IActionResult> DeleteSpeciality(
        [FromRoute] Guid id)
    {
        await _service.DeleteSpecialtyAsync(
            id);

        return Ok("ok");
    }
}