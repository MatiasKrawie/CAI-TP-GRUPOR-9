using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.DTOs;
using Products.API.Models;

namespace Products.API.Repositories;

public class ProductRepository
{
    private readonly IConfiguration _configuration;

    public ProductRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqliteConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=app.db";

        return new SqliteConnection(connectionString);
    }

    // GET ALL // Trae todos los productos y soporta filtros como el nombre y categoria.

    public async Task<IEnumerable<Product>> GetAllAsync(
        string? categoria,
        string? nombre)
    {
        using var connection = CreateConnection();

        var sql = """
            SELECT
                id,
                nombre,
                descripcion,
                precio,
                stock,
                categoria,
                fecha_creacion AS FechaCreacion
            FROM products
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
                precio,
                stock,
                categoria,
                fecha_creacion AS FechaCreacion
            FROM products
            WHERE id = @Id
        """;

        return await connection.QueryFirstOrDefaultAsync<Product>(
            sql,
            new { Id = id.ToString() });
    }

    // CREATE// inserta nuevo producto
    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        using var connection = CreateConnection();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Precio = request.Precio,
            Stock = request.Stock,
            Categoria = request.Categoria,
            FechaCreacion = DateTime.UtcNow
        };

        var sql = """
            INSERT INTO products
            (
                id,
                nombre,
                descripcion,
                precio,
                stock,
                categoria,
                fecha_creacion
            )
            VALUES
            (
                @Id,
                @Nombre,
                @Descripcion,
                @Precio,
                @Stock,
                @Categoria,
                @FechaCreacion
            )
        """;

        await connection.ExecuteAsync(sql, product);

        return product;
    }

    // UPDATE //actualiza producto
    public async Task<Product?> UpdateAsync(
        Guid id,
        UpdateProductRequest request)
    {
        using var connection = CreateConnection();

        var existing = await GetByIdAsync(id);

        if (existing is null)
            return null;

        var sql = """
            UPDATE products
            SET
                nombre = @Nombre,
                descripcion = @Descripcion,
                precio = @Precio,
                stock = @Stock,
                categoria = @Categoria
            WHERE id = @Id
        """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            request.Nombre,
            request.Descripcion,
            request.Precio,
            request.Stock,
            request.Categoria
        });

        return await GetByIdAsync(id);
    }

    // DELETE// elimina producto existente
    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = CreateConnection();

        var sql = """
            DELETE FROM products
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
            FROM products
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
