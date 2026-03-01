using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // TODO Week 2: Register, Login with BCrypt, JWT or session
    [HttpPost("register")]
    public IActionResult Register([FromBody] object model) => Ok(new { message = "Register - implement in Week 2" });

    [HttpPost("login")]
    public IActionResult Login([FromBody] object model) => Ok(new { message = "Login - implement in Week 2" });
}
