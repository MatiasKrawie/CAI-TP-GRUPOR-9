using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orders.Api.DTOs;

namespace Orders.Api.Services
{
    public interface IOrderService
    {
        // GET /api/orders 
        Task<IEnumerable<OrderResponse>> GetAllAsync(Guid? usuarioId);

        // GET /api/orders/{id}
        Task<OrderResponse> GetByIdAsync(Guid id);

        // GET /api/orders/{productoId}
        Task<bool> HasOrdersAsync(Guid productoId);

        // POST /api/orders
        Task<OrderResponse> CreateAsync(OrderRequest request);

        // PUT /api/orders/{id}/status -> Usa string para el estado
        Task<OrderResponse> UpdateStatusAsync(Guid id, string nuevoEstado);
    }
}