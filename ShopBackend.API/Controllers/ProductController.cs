using Microsoft.AspNetCore.Mvc;
using ShopBackend.Application.Interfaces;
using ShopBackend.Application.DTOs;


namespace ShopBackend.Api.Controllers
{
    [ApiController]
    // keine Umlaute bei den HTTP Routen die in Klammern stehen z.b. [HttpDelete("{id}/hard")] da diese in URLs sonst Prozentenkodiert ausgegeben werden. z.b. löschen -> l%C3%B6schen 
    [Route("api/[controller]")]

    public class ProductController : ControllerBase
    {

        private readonly IProductStockService _productStockService;

        public ProductController(IProductStockService productStockService)
        {
            _productStockService = productStockService;
        }



        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productStockService.GetAllAsync();
            return Ok(products);

        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productStockService.GetByIdAsync(id);
            return Ok(product);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetAllActive()
        {
            var products = await _productStockService.GetAllActiveAsync();
            return Ok(products);
        }


                /*CreatedAtAction gibt HTTP 201 zurück und setzt einen Location Header in der Response:

                    * nameof(GetById) -> der Name des Endpoints wo man das neue Produkt finden kann
                    * new { id = product.Id } -> der Route-Parameter für diesen Endpoint z.b. id = 5 (api/product/5)
                    * product -> das erstellte Objekt als Response Body
                    
                    Also die Response sagt praktisch: "Erstellt! Du findest es hier: api/product/5
                */
        [HttpPost]
        public async Task <IActionResult> Create(CreateProductDto dto)
        {
            var product = await _productStockService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
            
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductDto dto)
        {
            await _productStockService.UpdateAsync(id, dto);
            return NoContent();  // Http 204 Standard für erfolgreiche Updates
        }

        [HttpGet("{id}/stock")]
        public async Task<IActionResult> GetStockByProductId(int id)
        {
            var stock = await _productStockService.GetStockByProductIdAsync(id);
            return Ok(stock);
        }

        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, UpdateStockDto dto) // product.Id ist kein gültiger Parameter hier.
        {
            await _productStockService.UpdateStockAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            await _productStockService.SoftDeleteAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}/hard")]
        public async Task<IActionResult> HardDelete(int id)
        {
            await _productStockService.HardDeleteAsync(id);
            return NoContent();

        }


    }
}