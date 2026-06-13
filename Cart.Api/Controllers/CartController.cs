using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Cart.Api.DTOs;
using Cart.Api.Services;

namespace Cart.Api.Controllers
{
    [ApiController]
    [Route("api/cart")] 
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService _cartService)
        {
            this._cartService = _cartService;
        }

        // GET /api/cart/{userId}
        [HttpGet("{userId:guid}")] 
        public async Task<ActionResult<CartResponse>> GetCart(Guid userId)
        {
            var cart = await _cartService.GetByUserIdAsync(userId);
            return Ok(cart);
        }

        // POST /api/cart/{userId}/items
        [HttpPost("{userId:guid}/items")] 
        public async Task<ActionResult<CartResponse>> AddItem(Guid userId, [FromBody] CartItemRequest request)
        {
            var updatedCart = await _cartService.AddItemAsync(userId, request);
            return Ok(updatedCart);
        }

        // PUT /api/cart/{userId}/items/{productId}
        [HttpPut("{userId:guid}/items/{productId:guid}")] 
        public async Task<ActionResult<CartResponse>> UpdateItem(Guid userId, Guid productId, [FromBody] UpdateCantidadRequest request)
        {
            var updatedCart = await _cartService.UpdateItemCantidadAsync(userId, productId, request);
            return Ok(updatedCart);
        }

        // DELETE /api/cart/{userId}/items/{productId}
        [HttpDelete("{userId:guid}/items/{productId:guid}")] 
        public async Task<IActionResult> RemoveItem(Guid userId, Guid productId)
        {
            await _cartService.RemoveItemAsync(userId, productId);
            return NoContent();
        }

        // DELETE /api/cart/{userId}
        [HttpDelete("{userId:guid}")] 
        public async Task<IActionResult> ClearCart(Guid userId)
        {
            await _cartService.ClearCartAsync(userId);
            return NoContent();
        }
    }
}