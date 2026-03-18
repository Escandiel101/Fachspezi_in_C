using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;


namespace ShopBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll() // Admins haben über den Handler IMMER Zugriff, GetAll() nur für Admins
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [Authorize(Policy = "IsResourceOwner")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(user);
        }


        [AllowAnonymous] // Dominiert über Policy oder andere Authorizations, lässt alle durch.
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserDto dto)
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [Authorize(Policy = "IsResourceOwner")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateUserDto dto)
        {
            await _userService.UpdateAsync(id, dto);
            return NoContent();
        }

        [Authorize(Policy = "IsResourceOwner")]
        [HttpPut("{id}/changePw")]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordDto dto)
        {
            await _userService.ChangePasswordAsync(id, dto);
            return NoContent();
        }

        [Authorize(Policy = "IsResourceOwner")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteAsync(id);
            return NoContent(); 
        }

    }
}
