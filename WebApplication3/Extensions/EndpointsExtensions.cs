using WebApplication3.Models;

namespace WebApplication3.Extensions
{
    public static class EndpointsExtensions
    {

        public static void MapItemEndpoints(this WebApplication app)

        {

            var items = new List<Item>();

            var idCounter = 1L;


            // GET all
            
            app.MapGet("/items", () =>
            {

                return Results.Ok(items);

            })

        .WithTags("Items");


            // GET by id
            app.MapGet("/items/{id}", (long id) =>
            {

                var item = items.FirstOrDefault(i => i.Id == id);

                return item is not null ? Results.Ok(item) : Results.NotFound();

            })

        .WithTags("Items");


            // POST
            app.MapPost("/items", (CreateItemRequest req) =>
            {

                var item = new Item
                {

                    Id = idCounter++,

                    Name = req.Name,

                    Description = req.Description,

                    Price = (double)req.Price,

                    Stock = req.Stock,

                    CreatedAt = DateTime.UtcNow.ToString("o")

                };


                items.Add(item);


                return Results.Ok(item);

            })

        .WithTags("Items");


            // PUT
            app.MapPut("/items/{id}", (long id, UpdateItemRequest req) =>
            {

                var existing = items.FirstOrDefault(i => i.Id == id);


                if (existing is null)

                    return Results.NotFound();


                var updated = existing with
                {

                    Name = req.Name,

                    Description = req.Description,

                    Price = (double)req.Price,

                    Stock = req.Stock,

                    UpdatedAt = DateTime.UtcNow.ToString("o")

                };


                items.Remove(existing);

                items.Add(updated);


                return Results.Ok(updated);

            })

        .WithTags("Items");


            // DELETE
            app.MapDelete("/items/{id}", (long id) =>
            {

                var item = items.FirstOrDefault(i => i.Id == id);


                if (item is null)

                    return Results.NotFound();


                items.Remove(item);


                return Results.Ok();

            })

        .WithTags("Items");

        }

    }
}
