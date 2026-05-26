using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Cart.Api.DTOs;
using Cart.Api.Services;

namespace Cart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService _cartService)
        {
            this._cartService = _cartService;
        }

        // GET /api/cart/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<CartResponse>> GetCart(int userId)
        {
            var cart = await _cartService.GetByUserIdAsync(userId);
            return Ok(cart);
        }

        // POST /api/cart/{userId}/items
        [HttpPost("{userId}/items")]
        public async Task<ActionResult<CartResponse>> AddItem(int userId, [FromBody] CartItemRequest request)
        {
            var updatedCart = await _cartService.AddItemAsync(userId, request);
            return Ok(updatedCart);
        }

        // PUT /api/cart/{userId}/items/{productId}
        [HttpPut("{userId}/items/{productId}")]
        public async Task<ActionResult<CartResponse>> UpdateItem(int userId, int productId, [FromBody] UpdateCantidadRequest request)
        {
            var updatedCart = await _cartService.UpdateItemCantidadAsync(userId, productId, request);
            return Ok(updatedCart);
        }

        // DELETE /api/cart/{userId}/items/{productId}
        [HttpDelete("{userId}/items/{productId}")]
        public async Task<IActionResult> RemoveItem(int userId, int productId)
        {
            await _cartService.RemoveItemAsync(userId, productId);
            return NoContent(); 
        }

        // DELETE /api/cart/{userId}
        [HttpDelete("{userId}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            await _cartService.ClearCartAsync(userId);
            return NoContent(); 
        }
    }
}