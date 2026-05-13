using Dapper;
using Microsoft.Data.Sqlite;
using users.API.DTOs;
using users.API.Models;

namespace users.API.Repositories;

public class usersRepository
{
    private readonly IConfiguration _configuration;

    public usersRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqliteConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=app.db";

        return new SqliteConnection(connectionString);
    }

    // GET ALL // Trae todos los users y soporta filtros como el nombre y categoria.

    public async Task<IEnumerable<Product>> GetAllAsync(
        string? categoria,
        string? nombre)
    {
        using var connection = CreateConnection();

        var sql = """
            SELECT
                id,
                nombre,
                descripcion;
                categoria,
                fecha_creacion AS FechaCreacion
            FROM users
            WHERE
                (@Categoria IS NULL OR categoria = @Categoria)
                AND
                (@Nombre IS NULL OR nombre LIKE '%' || @Nombre || '%')
            ORDER BY fecha_creacion DESC
        """;

        return await connection.QueryAsync<Product>(sql, new
        {
            Categoria = categoria,
            Nombre = nombre
        });
    }

    // GET BY ID// busca por id
    public async Task<Product?> GetByIdAsync(Guid id)
    {
        using var connection = CreateConnection();

        var sql = """
            SELECT
                id,
                nombre,
                descripcion,
                categoria,
                fecha_creacion AS FechaCreacion
            FROM users
            WHERE id = @Id
        """;

        return await connection.QueryFirstOrDefaultAsync<user>(
            sql,
            new { Id = id.ToString() });
    }

    // CREATE// inserta nuevo user
    public async Task<user> CreateAsync(CreateuserRequest request)
    {
        using var connection = CreateConnection();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
          categoria= request.categoria;
            FechaCreacion = DateTime.UtcNow
        };

        var sql = """
            INSERT INTO users
            (
                id,
                nombre,
                descripcion;
          categoria;
                fecha_creacion
            )
            VALUES
            (
                @Id,
                @Nombre,
                @Descripcion,
                @Categoria,
                @FechaCreacion
            )
        """;

        await connection.ExecuteAsync(sql, user);

        return user;
    }

    // UPDATE //actualiza producto
    public async Task<user?> UpdateAsync(
        Guid id,
        UpdateuserRequest request)
    {
        using var connection = CreateConnection();

        var existing = await GetByIdAsync(id);

        if (existing is null)
            return null;

        var sql = """
            UPDATE user
            SET
                nombre = @Nombre,
                descripcion = @Descripcion,
                categoria = @Categoria
            WHERE id = @Id
        """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            request.Nombre,
            request.Descripcion,
            request.Categoria
        });

        return await GetByIdAsync(id);
    }

    // DELETE// elimina producto existente
    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = CreateConnection();

        var sql = """
            DELETE FROM users
            WHERE id = @Id
        """;

        var rows = await connection.ExecuteAsync(sql, new
        {
            Id = id.ToString()
        });

        return rows > 0;
    }

    // DUPLICATE CHECK// chequea si hay duplicados
    public async Task<bool> ExistsByNameAndCategoryAsync(
        string nombre,
        string categoria)
    {
        using var connection = CreateConnection();

        var sql = """
            SELECT COUNT(*)
            FROM users
            WHERE nombre = @Nombre
            AND categoria = @Categoria
        """;

        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            Nombre = nombre,
            Categoria = categoria
        });

        return count > 0;
    }
}
