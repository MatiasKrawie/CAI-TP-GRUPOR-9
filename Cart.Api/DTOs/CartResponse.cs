using System;
using System.Collections.Generic;

namespace Cart.Api.DTOs
{
    public class CartResponse
    {
        public Guid UsuarioId { get; set; } 
        public string FechaActualizacion { get; set; } = string.Empty;
        public List<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();
    }
}