using System.Collections.Generic;

namespace Orders.Api.DTOs
{
    public class OrderRequest
    {
        public int UsuarioId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
    }

    public class OrderItemRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}