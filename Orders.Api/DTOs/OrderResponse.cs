using System;
using System.Collections.Generic;

namespace Orders.Api.DTOs
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string FechaCreacion { get; set; } = string.Empty;
    }


    public class OrderItemResponse
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }


    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }


    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }

        public string? Category { get; set; }

        public string? Description { get; set; }
    }
}