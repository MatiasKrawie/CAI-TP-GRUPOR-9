namespace Orders.Api.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public string FechaCreacion { get; set; } = string.Empty;
    }
}