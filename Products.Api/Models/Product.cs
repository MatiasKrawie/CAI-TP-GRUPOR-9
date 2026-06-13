using System;

namespace Products.API.Models
{
    public class Product
    {
      
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; }

        
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string? UpdatedAt { get; set; }
    }
}