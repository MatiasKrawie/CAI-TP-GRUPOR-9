using System;

namespace Cart.Api.DTOs
{
    public class CartItemRequest
    {
        public Guid ProductoId { get; set; } 
        public int Cantidad { get; set; }
    }
}