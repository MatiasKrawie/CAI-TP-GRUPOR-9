using System;

namespace Cart.Api.Models
{
    public class CartDetail
    {
        public Guid UsuarioId { get; set; } // Cambiado a Guid
        public Guid ProductoId { get; set; } // Cambiado a Guid
        public int Cantidad { get; set; }
    }
}