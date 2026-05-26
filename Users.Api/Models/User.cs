namespace Users.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FechaRegistro { get; set; } = string.Empty;
        public int Activo { get; set; } = 1;
        public int IntentosFallidos { get; set; } = 0;
        public int BloqueoFraude { get; set; } = 0;
    }
}