using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.Interfaces;
using ShopBackend.Application.DTOs;
using Microsoft.AspNetCore.Authorization;




namespace ShopBackend.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class DiscountCodeController : ControllerBase    
    {
        private readonly IDiscountCodeService _discountCodeService;

        public DiscountCodeController(IDiscountCodeService discountCodeService)
        {
            _discountCodeService = discountCodeService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var discountCodes = await _discountCodeService.GetAllAsync();
            return Ok(discountCodes);
        }

        [Authorize] // Könnte man für user sperren, allerdings möchten die sicherlich auch gerne sehen ob Code xyz gültig ist und welche Vorrausetzungen ein Code hat. 
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dicountCode = await _discountCodeService.GetByIdAsync(id);
            return Ok(dicountCode);
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateDiscountCodeDto dto)
        {
            var discountCode = await _discountCodeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = discountCode.Id }, discountCode);
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateDiscountCodeDto dto)
        {
            await _discountCodeService.UpdateAsync(id, dto);
            return NoContent();
        }

    }
}
