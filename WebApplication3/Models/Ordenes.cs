namespace WebApplication3.Models
{

    // ── Entidad principal ─────────────────────────
    public record Ordenes
    {

        public long Id { get; init; }

        public string OrderNumber { get; init; } = string.Empty;

        public string? Reason { get; init; }

        public double SalesChannel { get; init; }


        public string CreatedAt { get; init; } = string.Empty;

        public string? UpdatedAt { get; init; }

    }


    // ── Request para crear un ítem (POST) ─────────────────────────────────────

    public record CreateOrderRequest(

    string Name,

        string? Description,

        decimal Price,

        int Stock);

    // ── Request para actualizar un ítem (PUT) ─────────────────────────────────
    public record UpdateOrderRequest(

   string Name,

       string? Description,

       decimal Price,

       int Stock);
}
