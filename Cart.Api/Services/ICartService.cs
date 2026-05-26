using System.Threading.Tasks;
using Cart.Api.DTOs;

namespace Cart.Api.Services
{
    public interface ICartService
    {
        Task<CartResponse> GetByUserIdAsync(int userId);
        Task<CartResponse> AddItemAsync(int userId, CartItemRequest request);
        Task<CartResponse> UpdateItemCantidadAsync(int userId, int productId, UpdateCantidadRequest request);
        Task RemoveItemAsync(int userId, int productId);
        Task ClearCartAsync(int userId);
    }
}