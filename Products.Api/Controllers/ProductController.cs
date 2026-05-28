using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Products.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // 1. GET: /api/products (Filtros opcionales)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? category, [FromQuery] string? name)
        {
            var products = await _productService.GetAllAsync(category, name);

            var response = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                Category = p.Category,
                CreatedAt = p.CreatedAt
            });

            return Ok(response);
        }

        // 2. GET: /api/products/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
           
            var product = await _productService.GetByIdAsync(id);

            var response = new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                CreatedAt = product.CreatedAt
            };

            return Ok(response);
        }

        // 3. POST: /api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
           
            if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0 || request.Stock <= 0)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "La estructura del cuerpo de la petición o los campos no cumplen las reglas.",
                    Instance = HttpContext.Request.Path
                };

                
                problemDetails.Extensions["errorCode"] = "PRD-002";
                problemDetails.Extensions["errorMessage"] = "Los datos del producto son inválidos.";

                return BadRequest(problemDetails);
            }

            var product = await _productService.CreateAsync(request);

            var response = new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                Category = product.Category,
                CreatedAt = product.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        // 4. PUT: /api/products/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0 || request.Stock <= 0)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Datos inválidos",
                    Detail = "Los datos del producto son inválidos."
                };
                problemDetails.Extensions["errorCode"] = "PRD-002";

                return BadRequest(problemDetails);
            }

           
            var updatedProduct = await _productService.UpdateAsync(id, request);

            var response = new ProductResponse
            {
                Id = updatedProduct.Id,
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                Price = updatedProduct.Price,
                Stock = updatedProduct.Stock,
                Category = updatedProduct.Category,
                CreatedAt = updatedProduct.CreatedAt
            };

            return Ok(response);
        }


        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int nuevoStock)
        {
            if (nuevoStock < 0)
                return BadRequest(new { message = "El stock no puede ser menor a cero." });

            await _productService.UpdateStockAsync(id, nuevoStock);

            return NoContent();
        }

        // 5. DELETE: /api/products/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
           
            var eliminado = await _productService.DeleteAsync(id);

            if (!eliminado)
            {
                return NotFound(new { Message = "El producto no existe o ya fue eliminado." });
            }

            return NoContent(); 
        }
    }
}