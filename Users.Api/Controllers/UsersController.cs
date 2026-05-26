using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Users.Api.DTOs;
using Users.Api.Services;

namespace Users.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // POST /api/users/register
        [HttpPost("register")]
        public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
        {
            var usuarioCreado = await _userService.RegisterAsync(request);
            // Retorna 201 Created
            return StatusCode(201, usuarioCreado);
        }

        // POST /api/users/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            var loginExitoso = await _userService.LoginAsync(request);
            // Retorna 200 OK
            return Ok(loginExitoso);
        }
    }
}