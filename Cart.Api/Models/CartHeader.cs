namespace Cart.Api.Models
{
    public class CartHeader
    {
        public int UsuarioId { get; set; } 
        public string FechaActualizacion { get; set; } = string.Empty;
    }
}