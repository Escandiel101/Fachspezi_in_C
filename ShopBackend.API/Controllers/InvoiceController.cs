using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.Interfaces;
using ShopBackend.Application.DTOs;
using Microsoft.AspNetCore.Authorization;


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


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _invoiceService.GetAllAsync();
            return Ok(invoices);
        }


        [Authorize(Policy = "IsResourceOwner")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            return Ok(invoice);
        }


        [Authorize(Policy = "IsResourceOwner")]
        [HttpGet("byOrderId/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var invoice = await _invoiceService.GetByOrderIdAsync(orderId);
            return Ok(invoice);
        }


        [Authorize(Policy = "IsResourceOwner")]
        [HttpGet("byCustomerId/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var invoices = await _invoiceService.GetByCustomerIdAsync(customerId);
            return Ok(invoices);
        }


        //[Authorize(Policy = "IsResourceOwner")]  Kunde muss Payment Method wählen, aber es gibt noch keine Invoice Id zur Orientierung --> Lösung über Handler verstehe ich nicht.
        [Authorize] // Ist Anfällig, Ein Kunde könnte Rechnungen für andere Kunden erstellen etc. --> Abfang im Service
        [HttpPost]
        public async Task<IActionResult> Create(CreateInvoiceDto dto)
        {
            var invoice = await _invoiceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }


        [Authorize(Roles = "Admin, Staff")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateInvoiceDto dto)
        {
            await _invoiceService.UpdateAsync(id, dto);
            return NoContent();
        }


    }
}
