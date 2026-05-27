using Products.API.DTOs;
using Products.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Products.API.Services
{
    public interface IProductService
    {
        // 1. GET /api/products 
        Task<IEnumerable<Product>> GetAllAsync(string? category, string? name);

        // 2. GET /api/products/{id}
        Task<Product> GetByIdAsync(int id);

        // 3. POST /api/products
        Task<Product> CreateAsync(CreateProductRequest request);

        // 4. PUT /api/products/{id}
        Task<Product> UpdateAsync(int id, UpdateProductRequest request);


        // 4. PUT /api/products/{id}/stock
        Task UpdateStockAsync(int id, int nuevoStock);

        // 5. DELETE /api/products/{id}
        Task<bool> DeleteAsync(int id);
    }
}