namespace Products.API.DTOs
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; } 
        public string CreatedAt { get; set; } = string.Empty;
    }
}