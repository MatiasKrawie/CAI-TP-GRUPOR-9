namespace Cart.Api.DTOs
{
    public class CartResponse
    {
        public int UsuarioId { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
        public string FechaActualizacion { get; set; } = string.Empty;
    }
}
