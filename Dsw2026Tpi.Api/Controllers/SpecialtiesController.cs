using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("specialties")]
[Authorize(Policy = Policies.AdminPolicy)]
public class SpecialtiesController : AppController
{
    private readonly ISpecialitiesService _service;
    
    public SpecialtiesController(ISpecialitiesService service)
    {
        _service = service;
    }
    
    //[HttpGet]
    
    //[HttpPost]

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSpeciality([FromRoute] Guid id, [FromBody] SpecialityModel.Request request)
    {
        await _service.UpdateSpecialitiy(id, request);
        return Ok();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSpeciality([FromRoute] Guid id)
    {
        await _service.DeleteSpecialitiy(id);
        return NoContent();
    }
}