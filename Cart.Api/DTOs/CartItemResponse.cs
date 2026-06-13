using System;

namespace Cart.Api.DTOs
{
    public class CartItemResponse
    {
        public Guid ProductoId { get; set; }
        public int Cantidad { get; set; }

        public string Nombre { get; set; } = string.Empty;
        public double Precio { get; set; }

        public double Subtotal => Precio * Cantidad;
    }
}