using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Notifications.Api.DTOs;
using Notifications.Api.Exceptions;

namespace Notifications.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly string _connectionString;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersUrl;

        public NotificationService(IConfiguration configuration, IHttpClientFactory clientFactory)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=notifications.db";
            _clientFactory = clientFactory;
            _usersUrl = configuration["UserServiceUrl"] ?? "https://localhost:7122"; 
        }

        private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

        public async Task<NotificationResponse> SendNotificationAsync(NotificationRequest request)
        {
            
            if (request.UsuarioId <= 0 || string.IsNullOrWhiteSpace(request.Mensaje) || string.IsNullOrWhiteSpace(request.Tipo))
                throw new NotFoundException("NTF-002", 400, "Los datos de la notificación son inválidos. Campos faltantes.");

            
            if (request.Tipo != "Email" && request.Tipo != "SMS" && request.Tipo != "Push")
                throw new NotFoundException("NTF-002", 400, "Los datos de la notificación son inválidos. Tipo no reconocido.");

            
            var client = _clientFactory.CreateClient();
            HttpResponseMessage userRes;
            try
            {
                userRes = await client.GetAsync($"{_usersUrl}/api/users/{request.UsuarioId}");
            }
            catch
            {
                throw new NotFoundException("NTF-004", 500, "Error de comunicación con Users.API.");
            }

            if (!userRes.IsSuccessStatusCode)
                throw new NotFoundException("NTF-001", 404, "El usuario destinatario no fue encontrado.");

           
            using var conn = CreateConnection();
            string sqlInsert = @"
                INSERT INTO Notificaciones (UsuarioId, Mensaje, Tipo, Estado, FechaEnvio)
                VALUES (@UsuarioId, @Mensaje, @Tipo, 'Enviada', @FechaEnvio);
                SELECT last_insert_rowid();";

            try
            {
                int nuevoId = await conn.QuerySingleAsync<int>(sqlInsert, new
                {
                    UsuarioId = request.UsuarioId,
                    Mensaje = request.Mensaje,
                    Tipo = request.Tipo,
                    FechaEnvio = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });

                return await conn.QuerySingleOrDefaultAsync<NotificationResponse>(
                    "SELECT Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio FROM Notificaciones WHERE Id = @Id",
                    new { Id = nuevoId });
            }
            catch (Exception)
            {
                throw new NotFoundException("NTF-004", 500, "Error interno al procesar y guardar la notificación.");
            }
        }

        public async Task<IEnumerable<NotificationResponse>> GetNotificationsByUserIdAsync(int userId)
        {
            using var conn = CreateConnection();
            var notifications = await conn.QueryAsync<NotificationResponse>(
                "SELECT Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio FROM Notificaciones WHERE UsuarioId = @UserId",
                new { UserId = userId });

            
            if (notifications == null || !notifications.Any())
                throw new NotFoundException("NTF-003", 404, "No se encontraron notificaciones para el usuario.");

            return notifications;
        }
    }
}