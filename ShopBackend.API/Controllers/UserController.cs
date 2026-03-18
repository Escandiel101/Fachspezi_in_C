using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;


namespace ShopBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "IsResourceOwner")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserDto dto)
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateUserDto dto)
        {
            await _userService.UpdateAsync(id, dto);
            return NoContent();
        }

        [Authorize]
        [HttpPut("{id}/changePw")]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordDto dto)
        {
            await _userService.ChangePasswordAsync(id, dto);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteAsync(id);
            return NoContent(); 
        }

    }
}
