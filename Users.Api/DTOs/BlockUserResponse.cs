using System;

namespace Users.Api.DTOs
{
    public class BlockUserResponse
    {
        public Guid Id { get; set; } // Cambiado de int a Guid
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int BloqueoFraude { get; set; }
    }
}