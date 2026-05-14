ECommerce Microservicios 
Trabajo Práctico CAI
Integrantes:
Sarah Senderosky
Owen Gribov
Matias kr…

Descripción del Proyecto: Este proyecto consiste en la implementación de una arquitectura basada en microservicios utilizando ASP.NET Core Web API en .NET 8. Se desarrollaron distintos microservicios independientes para manejar:
Productos
Usuarios
Ordenes
Carrito
Notificaciones
El objetivo principal es aplicar conceptos de:
APIs REST
Logging estructurado
Manejo global de errores
Swagger/OpenAPI
Health Checks
SQLite
Dapper
Correlation ID
Comunicación entre servicios

Tecnologías Utilizadas
Tecnología
Uso
.NET 8
Framework principal
ASP.NET Core Web API
Desarrollo de APIs REST
SQLite
Base de datos embebida
Dapper
Persistencia de datos
Serilog
Logging estructurado
Swagger/OpenAPI
Documentación interactiva
Health Checks
Monitoreo de estado
IExceptionHandler
Manejo global de errores
Visual Studio 2022
Entorno de desarrollo
GitHub
Versionado


Arquitectura General
Cliente / Swagger
        |
        v
+-------------------+
|   Products API    |
+-------------------+
        |
        v
      SQLite

+-------------------+
|    Users API      |
+-------------------+
        |
        v
      SQLite

+-------------------+
|    Orders API     |
+-------------------+
        |
        +-------------------+
        |                   |
        v                   v
 Products API          Users API

Estructura del Proyecto
ECommerce.sln
│
├── src/
│   ├── Products.API/
│   ├── Users.API/
│   ├── Orders.API/
│   ├── Cart.API/
│   └── Notifications.API/
│
├── docs/
└── README.md

Ejecución del Proyecto
1. Clonar repositorio
git clone https://github.com/usuario/repositorio.git
2. Abrir solución
Abrir: ECommerce.sln con Visual Studio 2022.
3. Restaurar paquetes NuGet
Visual Studio restaura automáticamente:
Serilog
Dapper
SQLite
Swagger
HealthChecks
4. Ejecutar proyecto
Presionar: F5

Endpoints Importantes
Swagger: https://localhost:xxxx/swagger

Health Checks:
https://localhost:xxxx/health
https://localhost:xxxx/health/live
https://localhost:xxxx/health/ready

Logging
Los logs se almacenan en: logs/audit.json
Características:
Logging estructurado
Correlation ID
Request logging
Logs de errores
Rotación diaria
Correlation ID
Cada request HTTP recibe un identificador único: X-Correlation-Id
Este identificador:
aparece en logs,
se propaga entre servicios,
se incluye en respuestas de error.
Manejo de Errores
Se implementó manejo global de errores mediante: IExceptionHandler
Tipos de excepciones:
NotFoundException
ValidationException
BusinessRuleException
GlobalExceptionHandler
Formato estándar:
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "El recurso solicitado no fue encontrado.",
  "instance": "/api/products/1",
  "errorCode": "PRD-001",
  "errorMessage": "Producto no encontrado.",
  "correlationId": "abc-123"
}


Products API
Funcionalidades
Crear productos8
Obtener productos
Actualizar productos
Eliminar productos
Validar duplicados
Validar stock

Users API
Objetivo
Gestionar:
registro de usuarios,
login,
bloqueo automático por intentos fallidos.

Modelo User
public class User
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; }
}


DTOs
RegisterUserRequest
public record RegisterUserRequest(
    string Nombre,
    string Apellido,
    string Email,
    string Password);

LoginRequest
public record LoginRequest(
    string Email,
    string Password);

Endpoints Users API
Registrar usuario
POST /api/users/register
Response 201
{
  "id": "guid",
  "nombre": "Maria",
  "apellido": "Gonzalez",
  "email": "maria@email.com",
  "fechaRegistro": "2024-01-01",
  "activo": true
}


Login
POST /api/users/login

Response 200
{
  "id": "guid",
  "nombre": "Maria",
  "apellido": "Gonzalez",
  "email": "maria@email.com"
}

Reglas de Negocio Users
Email único
Si el email ya existe: USR-001

Bloqueo automático
Luego de 3 intentos fallidos, el usuario, pasa  de Activo = false

PasswordHash
Nunca se devuelve en responses.

Lógica Login
1. Buscar usuario por email
2. Verificar si está activo
3. Comparar contraseña
4. Incrementar intentos fallidos si falla
5. Bloquear al llegar a 3
6. Resetear intentos si login correcto


Orders API. Objetivo: Gestionar órdenes de compra.

Modelo Order
public class Order
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; }
}



Modelo OrderItem
public class OrderItem
{
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

DTO CreateOrderRequest
public record CreateOrderRequest(
    Guid UsuarioId,
    List<CreateOrderItemRequest> Items);

DTO CreateOrderItemRequest
public record CreateOrderItemRequest(
    Guid ProductoId,
    int Cantidad);

Endpoints Orders API
Obtener órdenes
GET /api/orders

Obtener orden por ID
GET /api/orders/{id}

Crear orden
POST /api/orders

Ejemplo Request
{
  "usuarioId": "guid",
  "items": [
    {
      "productoId": "guid",
      "cantidad": 2
    }
  ]
}


Actualizar estado
PUT /api/orders/{id}/status

Ejemplo Request
{
  "estado": "Confirmada"
}

Estados Permitidos
Pendiente
Confirmada
Enviada
Entregada
Cancelada

Validaciones Orders
Usuario inexistente
ORD-003
Producto inexistente
ORD-004
Stock insuficiente
ORD-005
Transición inválida
ORD-006

Comunicación entre Microservicios. Orders API se comunica con:
Users API
Products API
utilizando: IHttpClientFactory

Ejemplo Comunicación HTTP
var client = _httpClientFactory.CreateClient();

var response = await client.GetAsync(
    $"https://localhost:7001/api/products/{id}");

Health Checks: Cada microservicio implementa:
/health
/health/live
/health/ready
Se verifica:
estado de la API,
conectividad SQLite,
disponibilidad del servicio.

Swagger / OpenAPI. Swagger permite:
visualizar endpoints,
probar requests,
ver modelos,
validar códigos HTTP,
documentar errores.
Disponible en: /swagger

Serilog: Se implementó logging estructurado mediante Serilog. Características:
logs en consola,
logs JSON,
logs de requests,
logs de errores,
Correlation ID,
duración de requests.
SQLite
Se utilizó SQLite como base de datos embebida. Ventajas:
simple,
portable,
sin instalación,
ideal para proyectos académicos.

Dapper: Dapper se utilizó como micro ORM. Permite:
ejecutar SQL directamente,
mapear resultados a objetos C#,
mejor performance,
simplicidad.

Conclusión: El proyecto permitió aplicar conceptos de:
arquitectura de microservicios,
APIs REST,
observabilidad,
persistencia,
manejo global de errores,
documentación OpenAPI,
logging estructurado.
También permitió comprender:
la comunicación entre servicios,
trazabilidad mediante Correlation ID,
separación de responsabilidades,
buenas prácticas de backend.
