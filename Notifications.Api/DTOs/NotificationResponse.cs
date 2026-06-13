using System;

namespace Notifications.Api.DTOs
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaEnvio { get; set; } = string.Empty;
    }
}