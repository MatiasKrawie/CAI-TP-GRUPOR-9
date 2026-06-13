using System;
using System.Collections.Generic;

namespace Orders.Api.DTOs
{
    public class CartResponse
    {
        public Guid UsuarioId { get; set; } 
        public string FechaActualizacion { get; set; } = string.Empty;
        public List<CartItemResponse> Items { get; set; } = new();
    }

    public class CartItemResponse
    {
        public Guid ProductoId { get; set; }
        public int Cantidad { get; set; }
        public string Nombre { get; set; } = string.Empty; 
        public double Precio { get; set; }                  
    }
}