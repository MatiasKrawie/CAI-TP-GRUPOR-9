using System;

namespace Products.API.DTOs
{
    public class ProductResponse
    {
        // Cambiado a Guid para que no explote el mapeo de la respuesta
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}