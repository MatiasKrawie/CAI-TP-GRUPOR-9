using System;
using System.Threading.Tasks;
using Cart.Api.DTOs;

namespace Cart.Api.Services
{
    public interface ICartService
    {
        Task<CartResponse> GetByUserIdAsync(Guid userId);
        Task<CartResponse> AddItemAsync(Guid userId, CartItemRequest request);
        Task<CartResponse> UpdateItemCantidadAsync(Guid userId, Guid productId, UpdateCantidadRequest request);
        Task RemoveItemAsync(Guid userId, Guid productId);
        Task ClearCartAsync(Guid userId);
    }
}