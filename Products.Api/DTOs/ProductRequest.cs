namespace Products.API.DTOs
{
    // 1.POST /api/products
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; } 
        public int Stock { get; set; }    
        public string? Category { get; set; }
    }

    // PUT /api/products/{id}
    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Price { get; set; } 
        public int Stock { get; set; }    
        public string? Category { get; set; }
    }

    public class UpdateStockRequest
    {
        public int NuevoStock { get; set; }
    }
}