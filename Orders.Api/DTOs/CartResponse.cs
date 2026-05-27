using System.Collections.Generic;

namespace Orders.Api.DTOs
{
    public class CartResponse
    {
        public int UsuarioId { get; set; }
        public string FechaActualizacion { get; set; } = string.Empty;
        public List<CartItemResponse> Items { get; set; } = new();
    }

    public class CartItemResponse
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }
}