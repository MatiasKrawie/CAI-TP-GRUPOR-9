using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Repositories;

namespace Products.API.Services;

public class ProductService
{
    private readonly ProductRepository _repository;

    public ProductService(ProductRepository repository)
    {
        _repository = repository;
    }

    // GET ALL
    public async Task<IEnumerable<Product>> GetAllAsync(
        string? categoria,
        string? nombre)
    {
        return await _repository.GetAllAsync(categoria, nombre);
    }

    // GET BY ID
    public async Task<Product> GetByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);

        if (product is null)
        {
            throw new NotFoundException(
                "PRD-001",
                "Producto no encontrado.");
        }

        return product;
    }

    // CREATE
    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        var exists = await _repository.ExistsByNameAndCategoryAsync(
            request.Nombre,
            request.Categoria);

        if (exists)
        {
            throw new BusinessRuleException(
                "PRD-003",
                $"Ya existe un producto con ese nombre en la categoría '{request.Categoria}'.");
        }

        return await _repository.CreateAsync(request);
    }

    // UPDATE
    public async Task<Product> UpdateAsync(
        Guid id,
        UpdateProductRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);

        if (existing is null)
        {
            throw new NotFoundException(
                "PRD-001",
                "Producto no encontrado.");
        }

        var updated = await _repository.UpdateAsync(id, request);

        return updated!;
    }

    // DELETE
    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);

        if (existing is null)
        {
            throw new NotFoundException(
                "PRD-001",
                "Producto no encontrado.");
        }

        // Acá después podría ir:
        // validación de órdenes activas (PRD-004)

        await _repository.DeleteAsync(id);
    }
}
