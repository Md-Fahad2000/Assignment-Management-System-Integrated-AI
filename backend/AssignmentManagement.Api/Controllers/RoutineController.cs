using AssignmentManagement.Api.Models;
using AssignmentManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssignmentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoutineController : ControllerBase
{
    private readonly IRoutineService _routine;

    public RoutineController(IRoutineService routine) => _routine = routine;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _routine.GetByUserIdAsync(UserId);
        return Ok(list.Select(Map));
    }

    [HttpGet("day/{dayOfWeek:int}")]
    public async Task<IActionResult> GetByDay(int dayOfWeek)
    {
        if (dayOfWeek < 0 || dayOfWeek > 6) return BadRequest();
        var list = await _routine.GetByUserAndDayAsync(UserId, dayOfWeek);
        return Ok(list.Select(Map));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var s = await _routine.GetByIdAsync(id, UserId);
        if (s == null) return NotFound();
        return Ok(Map(s));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoutineSlotRequest model)
    {
        var s = await _routine.CreateAsync(UserId, model);
        if (s == null) return BadRequest(new { message = "Invalid input." });
        return Ok(Map(s));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoutineSlotRequest model)
    {
        var s = await _routine.UpdateAsync(id, UserId, model);
        if (s == null) return NotFound();
        return Ok(Map(s));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _routine.DeleteAsync(id, UserId)) return NotFound();
        return NoContent();
    }

    private static object Map(RoutineSlot s) => new
    {
        s.Id,
        s.UserId,
        day_of_week = s.DayOfWeek,
        start_time = s.StartTime.ToString(@"hh\:mm"),
        end_time = s.EndTime.ToString(@"hh\:mm"),
        s.Title,
        s.Description
    };
}
