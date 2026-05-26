namespace Notifications.Api.DTOs
{
    public class NotificationRequest
    {
        public int UsuarioId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; 
    }
}
