using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;


namespace ShopBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _customerService.GetAllAsync();
            return Ok(customers);
        }

        [Authorize(Policy = "IsResourceOwner")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            return Ok(customer);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("findBy/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            var customer = await _customerService.GetByEmailAsync(email);
            return Ok(customer);

        }

        [Authorize(Policy = "IsResourceOwner")]
        [HttpPost("{userId}")]
        public async Task<IActionResult> Create(int userId, CreateCustomerDto dto)
        {
            var customer = await _customerService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }

        [Authorize(Policy = "IsResourceOwner")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateCustomerDto dto)
        {
            await _customerService.UpdateAsync(id, dto);
            return NoContent();
        }

        [Authorize(Roles ="Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _customerService.DeleteAsync(id);
            return NoContent(); 
        }
    }
}

