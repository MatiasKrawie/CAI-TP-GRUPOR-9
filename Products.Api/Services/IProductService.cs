using Products.API.DTOs;
using Products.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Products.API.Services
{
    public interface IProductService
    {
        // 1. GET /api/products 
        Task<IEnumerable<Product>> GetAllAsync(string? category, string? name);

        // 2. GET /api/products/{id}
        Task<Product> GetByIdAsync(Guid id);

        // 3. POST /api/products
        Task<Product> CreateAsync(CreateProductRequest request);

        // 4. PUT /api/products/{id}
        Task<Product> UpdateAsync(Guid id, UpdateProductRequest request);

        // 5. PATCH /api/products/{id}/stock
        Task UpdateStockAsync(Guid id, int nuevoStock);

        // 6. DELETE /api/products/{id}
        Task<bool> DeleteAsync(Guid id);
    }
}