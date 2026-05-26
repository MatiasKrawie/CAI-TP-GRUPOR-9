namespace Notifications.Api.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaEnvio { get; set; } = string.Empty;
    }
}