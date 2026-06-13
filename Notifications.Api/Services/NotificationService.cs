using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Notifications.Api.DTOs;
using Notifications.Api.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Notifications.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly string _connectionString;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersUrl;

        static NotificationService()
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

        public NotificationService(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=notifications.db";
            _clientFactory = clientFactory;
            _usersUrl = configuration["UserServiceUrl"] ?? "https://localhost:7058";
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<NotificationResponse> SendNotificationAsync(NotificationRequest request)
        {
            if (request.UsuarioId == Guid.Empty || string.IsNullOrWhiteSpace(request.Mensaje) || string.IsNullOrWhiteSpace(request.Tipo))
                throw new ValidationException("NTF-002", "Los datos de la notificación son inválidos. Campos faltantes.");

            var tiposValidos = new[] { "Email", "SMS", "Push" };
            if (!tiposValidos.Contains(request.Tipo))
                throw new ValidationException("NTF-002", "Tipo de notificación no reconocido.");

            await ValidateUserExistsAsync(request.UsuarioId);

            Guid nuevoId = Guid.NewGuid();
            using var conn = CreateConnection();

            string sqlInsert = @"
                INSERT INTO Notificaciones (Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio)
                VALUES (@Id, @UsuarioId, @Mensaje, @Tipo, 'Enviada', @FechaEnvio);";

            try
            {
                await conn.ExecuteAsync(sqlInsert, new
                {
                    Id = nuevoId.ToString(),
                    UsuarioId = request.UsuarioId.ToString(),
                    request.Mensaje,
                    request.Tipo,
                    FechaEnvio = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });

                return await GetByIdAsync(nuevoId);
            }
            catch (Exception ex)
            {
                if (ex is NotFoundException) throw;
                throw new BusinessRuleException("NTF-004", $"Error interno de base de datos: {ex.Message}", 500);
            }
        }

        private async Task<NotificationResponse> GetByIdAsync(Guid id)
        {
            using var conn = CreateConnection();

            var ntf = await conn.QueryFirstOrDefaultAsync<NotificationResponse>(
                "SELECT Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio FROM Notificaciones WHERE Id = @Id",
                new { Id = id.ToString() });

            if (ntf == null)
                throw new NotFoundException("NTF-003", $"La notificación con ID {id} no existe.");

            return ntf;
        }

        private async Task ValidateUserExistsAsync(Guid userId)
        {
            var client = _clientFactory.CreateClient("UsersClient");
            HttpResponseMessage response;

            try
            {
                response = await client.GetAsync($"{_usersUrl}/api/users/{userId}");
            }
            catch (Exception)
            {
                throw new BusinessRuleException("NTF-005", "Falla de red al contactar Users.API", 500);
            }

            if (!response.IsSuccessStatusCode)
                throw new NotFoundException("NTF-001", $"El usuario {userId} no existe.");
        }

        public async Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(Guid userId)
        {
            using var conn = CreateConnection();

            var notifications = await conn.QueryAsync<NotificationResponse>(
                "SELECT Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio FROM Notificaciones WHERE UsuarioId = @UserId",
                new { UserId = userId.ToString() });

            return notifications;
        }

        public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value.ToString();
            public override Guid Parse(object value) => Guid.Parse((string)value);
        }
    }
}