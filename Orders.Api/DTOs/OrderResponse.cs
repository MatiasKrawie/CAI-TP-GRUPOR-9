using System;
using System.Collections.Generic;

namespace Orders.Api.DTOs
{
    public class OrderResponse
    {
        public Guid Id { get; set; } 
        public Guid UsuarioId { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string FechaCreacion { get; set; } = string.Empty;
    }

    public class OrderItemResponse
    {
        public Guid ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public class ProductDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; } 
        public int Stock { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
    }
}