using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/doctors")]
[Authorize]
public class DoctorController : AppController
{
    private readonly IDoctorService _service;

    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize, [FromQuery] int pageIndex, [FromQuery] string? name = null, [FromQuery] Guid? specialtyId = null)
    {
        var doctors = await _service.GetAllDoctorsAsync(pageSize, pageIndex, name, specialtyId);
        return Ok(doctors);
    }

    [HttpGet("{id}/availabilities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var doctor = await _service.GetDoctorAvailabilitiesAsync(id);
        return Ok(doctor);
    }
    
    [HttpGet("{id}/slots")]
    [Authorize]
    [ProducesResponseType(typeof(List<DoctorModel.SlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlots(
        [FromRoute] Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var slots = await _service.GetDoctorSlotsAsync(id, from, to);
        return Ok(slots);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] DoctorModel.Request request)
    {
        var created = await _service.CreateDoctorAsync(request);
        return CreatedAtAction(nameof(GetAll), created);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(
    typeof(DoctorModel.Response),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDoctors(
    [FromRoute] Guid id,
    [FromBody] DoctorModel.Request request)
    {
        var updatedDoctor =
            await _service.UpdateDoctorAsync(
                id,
                request);

        return Ok(updatedDoctor);
    }


    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDoctor(
        [FromRoute] Guid id)
    {
        await _service.DeleteDoctorAsync(
            id);

        return Ok("ok");
    }
}