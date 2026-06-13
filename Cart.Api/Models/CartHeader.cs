using System;

namespace Cart.Api.Models
{
    public class CartHeader
    {
        public Guid UsuarioId { get; set; } // Cambiado a Guid
        public string FechaActualizacion { get; set; } = string.Empty;
    }
}