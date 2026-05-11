using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
using Products.API.Models;
using Products.API.Services;

namespace Products.API.Controllers;

[ApiController]
[Route("api/products")]
[Tags("Products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _service;

    public ProductsController(ProductService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene todos los productos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? categoria,
        [FromQuery] string? nombre)
    {
        var products = await _service.GetAllAsync(categoria, nombre);

        return Ok(products);
    }

    /// <summary>
    /// Obtiene un producto por ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);

        return Ok(product);
    }

    /// <summary>
    /// Crea un nuevo producto.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Product), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request)
    {
        var product = await _service.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    /// <summary>
    /// Actualiza un producto existente.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request)
    {
        var product = await _service.UpdateAsync(id, request);

        return Ok(product);
    }

    /// <summary>
    /// Elimina un producto.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        return NoContent();
    }
}
