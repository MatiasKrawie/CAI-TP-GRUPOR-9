using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Threading.Tasks;
using Users.Api.DTOs;
using Users.Api.Exceptions;
using Users.Api.Models;

namespace Users.Api.Services
{
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        public UserService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=users.db";
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<UserResponse> RegisterAsync(RegisterRequest request)
        {
            
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Nombre))
            {
                throw new NotFoundException("USR-002", 400, "Los datos del usuario son inválidos.");
            }

            using var conn = CreateConnection();

           
            var existe = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT 1 FROM Usuarios WHERE Email = @Email", new { Email = request.Email });

            if (existe.HasValue)
                throw new NotFoundException("USR-001", 409, $"El email '{request.Email}' ya está registrado.");

            var fechaActual = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string sql = @"
                INSERT INTO Usuarios (Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos, BloqueoFraude)
                VALUES (@Nombre, @Apellido, @Email, @PasswordHash, @FechaRegistro, 1, 0, 0);
                SELECT last_insert_rowid();";

            int nuevoId = await conn.QuerySingleAsync<int>(sql, new
            {
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Email = request.Email,
                PasswordHash = request.Password, 
                FechaRegistro = fechaActual
            });

            return new UserResponse
            {
                Id = nuevoId,
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Email = request.Email,
                FechaRegistro = fechaActual,
                Activo = true
            };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new NotFoundException("USR-002", 400, "Los datos del usuario son inválidos.");

            using var conn = CreateConnection();

            
            var usuario = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Usuarios WHERE Email = @Email", new { Email = request.Email });

           
            if (usuario == null)
                throw new NotFoundException("USR-003", 401, "Credenciales incorrectas.");

           
            if (usuario.BloqueoFraude == 1)
                throw new NotFoundException("USR-005", 403, "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.");

          
            if (usuario.Activo == 0 || usuario.IntentosFallidos >= 3)
                throw new NotFoundException("USR-004", 403, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");

     
            if (usuario.PasswordHash != request.Password)
            {
              
                int nuevosIntentos = usuario.IntentosFallidos + 1;
                int nuevoEstadoActivo = nuevosIntentos >= 3 ? 0 : 1;

                await conn.ExecuteAsync(
                    "UPDATE Usuarios SET IntentosFallidos = @Intentos, Activo = @Activo WHERE Id = @Id",
                    new { Intentos = nuevosIntentos, Activo = nuevoEstadoActivo, Id = usuario.Id });

                if (nuevoEstadoActivo == 0)
                {
                    throw new NotFoundException("USR-004", 403, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
                }

                throw new NotFoundException("USR-003", 401, "Credenciales incorrectas.");
            }

          
            if (usuario.IntentosFallidos > 0)
            {
                await conn.ExecuteAsync("UPDATE Usuarios SET IntentosFallidos = 0 WHERE Id = @Id", new { Id = usuario.Id });
            }

            return new LoginResponse
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email
            };
        }

        public async Task<UserResponse> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();

           
            string sql = "SELECT Id, Nombre, Apellido, Email, FechaRegistro, Activo FROM Usuarios WHERE Id = @Id";
            var usuario = await conn.QueryFirstOrDefaultAsync<UserResponse>(sql, new { Id = id });

            
            if (usuario == null)
            {
                throw new NotFoundException("USR-006", 404, $"El usuario con ID {id} no fue encontrado.");
            }

            return usuario;
        }
    }
}