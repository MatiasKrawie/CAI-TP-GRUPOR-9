using System;

namespace Orders.Api.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public string FechaCreacion { get; set; } = string.Empty;
    }
}