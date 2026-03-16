using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.Interfaces;
using ShopBackend.Application.DTOs;




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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var discountCodes = await _discountCodeService.GetAllAsync();
            return Ok(discountCodes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dicountCode = await _discountCodeService.GetByIdAsync(id);
            return Ok(dicountCode);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateDiscountCodeDto dto)
        {
            var discountCode = await _discountCodeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = discountCode.Id }, discountCode);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateDiscountCodeDto dto)
        {
            await _discountCodeService.UpdateAsync(id, dto);
            return NoContent();
        }

    }
}
