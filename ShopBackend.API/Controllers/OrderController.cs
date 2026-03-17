using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.Interfaces;
using ShopBackend.Application.DTOs;
using Microsoft.AspNetCore.Authorization;



namespace ShopBackend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }



        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(orders);
        }


        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderService.GetByIdAsync(id);
            return Ok(order);
        }

        [HttpGet("{orderId}/orderItems")]
        public async Task<IActionResult> GetOrderItemsByOrderId(int orderId)
        {
            var orderItems = await _orderService.GetOrderItemsByOrderIdAsync(orderId);
            return Ok(orderItems);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderDto dto)
        {
            var order = await _orderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new {id = order.Id}, order);
        }

        [HttpPost("{orderId}/addOrderItem")]
        public async Task<IActionResult> AddOrderItem(int orderId, CreateOrderItemDto dto)
        {
            // nicht jeder Post muss ein 201 Created zurückgeben, hier wird zwar ein OrderItem im Service für die DB erstellt, aber es erfolgt kein direkter Rückgabewert, sondern sozusagen ein Put im Post-Pelz
            await _orderService.AddOrderItemAsync(orderId, dto);
            // deswegen NoContent() oder Ok() statt CreatedAtAction(...); 
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateOrderDto dto)
        {
            await _orderService.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _orderService.CancelAsync(id);
            return NoContent();
        }

        // Die Ausgabe in der URL wäre hier z.B. bei order id=5 und orderItem id=3 :  api/order/5/orderItem/3
        [HttpPut("{orderId}/orderItem/{orderItemId}")]
        public async Task<IActionResult> UpdateOrderItem(int orderId, int orderItemId, UpdateOrderItemDto dto)
        {
            await _orderService.UpdateOrderItemAsync(orderId, orderItemId, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _orderService.DeleteAsync(id);
            return NoContent();
        }


        [HttpDelete("{orderId}/orderItem/{orderItemId}")]
        public async Task<IActionResult> RemoveOrderItem(int orderId, int orderItemId)
        {
            await _orderService.RemoveOrderItemAsync(orderId, orderItemId);
            return NoContent();
        }

    }
}
