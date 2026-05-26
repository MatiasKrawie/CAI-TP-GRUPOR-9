namespace Users.Api.DTOs
{
    public class UserResponse
    {
        public int Id { get; set; } 
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FechaRegistro { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
