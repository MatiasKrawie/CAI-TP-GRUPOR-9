using WebApplication3.Models;
using WebApplication3.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Extensions

{
    public static class EndpointsExtensions
    {

        public static void MapProductEndpoints(this WebApplication app)

        {


            // GET all
            app.MapGet("/products", async (ProductRepository repo) =>
            {
                var products = await repo.GetAllAsync();
                return Results.Ok(products);
            })
            .WithTags("Products");



            // GET by id
            app.MapGet("/products/{id}", async (int id, ProductRepository repo) =>
            {
                var product = await repo.GetByIdAsync(id);
                return product is not null ? Results.Ok(product) : Results.NotFound();
            })
                      .WithTags("Products");


            // POST
            app.MapPost("/products", async (CreateProductRequest req, ProductRepository repo) =>
            {
                // Validación básica
                if (string.IsNullOrWhiteSpace(req.Name))
                {
                    return Results.BadRequest("El nombre es obligatorio.");
                }
          
                var nuevoProduct = await repo.CreateAsync(req);

                return Results.Created($"/products/{nuevoProduct.Id}", nuevoProduct);
            })
            .WithTags("Products");

            // PUT
            app.MapPut("/products/{id}", async (int id, UpdateProductRequest req, ProductRepository repo) =>
            {
                if (string.IsNullOrWhiteSpace(req.Name))
                {
                    return Results.BadRequest("El nombre es obligatorio.");
                }
               
                var productActualizado = await repo.UpdateAsync(id, req);
               
                return productActualizado is not null
                    ? Results.Ok(productActualizado)
                    : Results.NotFound($"No se encontró ningún Producto con el ID {id}.");
            })
            .WithTags("Products");


            // DELETE
            app.MapDelete("/products/{id}", async (int id, ProductRepository repo) =>
            {
                var productEliminado = await repo.DeleteAsync(id);

                return productEliminado is not null
                    ? Results.Ok(productEliminado)
                    : Results.NotFound($"No se encontró ningún Producto con el ID {id} para eliminar.");
            })
            .WithTags("Products");

        }

    }
}
