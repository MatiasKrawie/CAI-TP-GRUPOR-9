using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orders.Api.DTOs;

namespace Orders.Api.Services
{
    public interface IOrderService
    {
        // GET /api/orders 
        Task<IEnumerable<OrderResponse>> GetAllAsync(int? usuarioId);

        // GET /api/orders/{id}
        Task<OrderResponse> GetByIdAsync(int id);

        // GET /api/orders/{productoId}
        Task<bool> HasOrdersAsync(int productoId);

        // POST /api/orders
        Task<OrderResponse> CreateAsync(OrderRequest request);

        // PUT /api/orders/{id}/status
        Task<OrderResponse> UpdateStatusAsync(int id, string nuevoEstado);
    }
}