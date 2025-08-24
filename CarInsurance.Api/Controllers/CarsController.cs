using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid([FromRoute] long carId, [FromQuery] string date)
    {
        if (carId <= 0)
            return BadRequest("CarId must be > 0.");


        if (!DateOnly.TryParse(date, out var parsed))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");


        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }


    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<ClaimDto>> RegisterClaim([FromRoute] long carId, [FromBody] CreateClaimDto dto)
    {
        if (!DateOnly.TryParse(dto.ClaimDate, out var claimDate))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        if (dto.Amount <= 0)
            return BadRequest("Amount must be > 0.");

        try
        {
            var created = await _service.RegisterClaimAsync(carId, claimDate, dto.Description, dto.Amount);
            return CreatedAtAction(nameof(GetHistory), new { carId }, created);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<List<HistoryItemDto>>> GetHistory([FromRoute] long carId)
    {
        try
        {
            var items = await _service.GetHistoryAsync(carId);
            return Ok(items);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
