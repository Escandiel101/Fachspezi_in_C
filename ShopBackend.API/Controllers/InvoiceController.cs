using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.Interfaces;
using ShopBackend.Application.DTOs;


namespace ShopBackend.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }



        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _invoiceService.GetAllAsync();
            return Ok(invoices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            return Ok(invoice);
        }

        [HttpGet("byOrderId/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var invoice = await _invoiceService.GetByOrderIdAsync(orderId);
            return Ok(invoice);
        }

        [HttpGet("byCustomerId/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var invoices = await _invoiceService.GetByCustomerIdAsync(customerId);
            return Ok(invoices);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateInvoiceDto dto)
        {
            var invoice = await _invoiceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateInvoiceDto dto)
        {
            await _invoiceService.UpdateAsync(id, dto);
            return NoContent();
        }


    }
}
