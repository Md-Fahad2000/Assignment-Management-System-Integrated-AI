using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentsController : ControllerBase
{
    // TODO Week 2: CRUD with MySQL
    [HttpGet]
    public IActionResult GetAll() => Ok(Array.Empty<object>());

    [HttpPost]
    public IActionResult Create([FromBody] object model) => Ok(new { id = 1, message = "Implement in Week 2" });
}
