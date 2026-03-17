using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;


namespace ShopBackend.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class UserAuthController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserAuthController(IUserService userService)
        {
            _userService = userService;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var login = await _userService.LoginAsync(dto);
            return Ok(login);
        }
    }
}

