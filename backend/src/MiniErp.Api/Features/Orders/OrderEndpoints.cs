using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Common;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Features.Orders;

public static class OrderEndpoints
{
    public record OrderSummaryDto(
        Guid Id, int Number, string CustomerName, string Status, decimal Total, int ItemCount,
        string SellerName, DateTime CreatedAt, DateTime? ConfirmedAt);

    public record OrderItemDto(Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal Total);

    public record OrderDto(
        Guid Id, int Number, string CustomerName, string? CustomerDocument, string? Notes, string Status,
        decimal Total, Guid SellerId, string SellerName, DateTime CreatedAt, DateTime? ConfirmedAt,
        DateTime? CancelledAt, IEnumerable<OrderItemDto> Items);

    public record CreateOrderItem(Guid ProductId, int Quantity);
    public record CreateOrderRequest(string CustomerName, string? CustomerDocument, string? Notes,
        List<CreateOrderItem> Items, bool Confirm = false);

    public class CreateValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateValidator()
        {
            RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150).WithName("Cliente");
            RuleFor(x => x.CustomerDocument).MaximumLength(20).WithName("Documento");
            RuleFor(x => x.Notes).MaximumLength(500).WithName("Observações");
            RuleFor(x => x.Items).NotEmpty().WithMessage("Adicione ao menos um item ao pedido.");
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId).NotEmpty().WithName("Produto");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithName("Quantidade");
            });
        }
    }

    public static void MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization(Policies.Sell);

        group.MapGet("/", List);
        group.MapGet("/{id:guid}", Get);
        group.MapPost("/", Create).Validate<CreateOrderRequest>();
        group.MapPost("/{id:guid}/confirm", Confirm);
        group.MapPost("/{id:guid}/cancel", Cancel);
    }

    private static async Task<PagedResult<OrderSummaryDto>> List(AppDbContext db, ClaimsPrincipal user,
        string? search, OrderStatus? status, DateOnly? from, DateOnly? to, int? page, int? pageSize,
        CancellationToken ct)
    {
        var query = VisibleOrders(db, user);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = int.TryParse(term, out var number)
                ? query.Where(o => o.Number == number)
                : query.Where(o => EF.Functions.ILike(o.CustomerName, $"%{term}%"));
        }

        if (status is not null)
            query = query.Where(o => o.Status == status);
        if (from is not null)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= start);
        }
        if (to is not null)
        {
            var end = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt < end);
        }

        return await query
            .OrderByDescending(o => o.Number)
            .Select(o => new OrderSummaryDto(o.Id, o.Number, o.CustomerName, o.Status.ToString(), o.Total,
                o.Items.Count, o.Seller!.Name, o.CreatedAt, o.ConfirmedAt))
            .ToPagedAsync(page, pageSize, ct);
    }

    private static async Task<IResult> Get(Guid id, AppDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var order = await VisibleOrders(db, user)
            .Include(o => o.Items)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? Results.NotFound() : Results.Ok(ToDto(order));
    }

    private static async Task<IResult> Create(CreateOrderRequest req, AppDbContext db, ClaimsPrincipal user,
        CancellationToken ct)
    {
        var ids = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var missing = ids.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return Results.Problem(statusCode: 422, title: "Produto não encontrado",
                detail: $"Produto(s) inexistente(s): {string.Join(", ", missing)}");

        var order = new SalesOrder
        {
            CustomerName = req.CustomerName.Trim(),
            CustomerDocument = req.CustomerDocument?.Trim(),
            Notes = req.Notes,
            SellerId = user.UserId()
        };

        foreach (var item in req.Items)
            order.AddItem(products[item.ProductId], item.Quantity);

        db.SalesOrders.Add(order);

        if (req.Confirm)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.SaveChangesAsync(ct);
            db.StockMovements.AddRange(order.Confirm(products, user.UserId()));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }

        return Results.Created($"/api/orders/{order.Id}", new { order.Id, order.Number });
    }

    private static async Task<IResult> Confirm(Guid id, AppDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var order = await VisibleOrders(db, user).Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return Results.NotFound();

        var products = await LoadProducts(db, order, ct);
        db.StockMovements.AddRange(order.Confirm(products, user.UserId()));
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> Cancel(Guid id, AppDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var order = await VisibleOrders(db, user).Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return Results.NotFound();

        var products = await LoadProducts(db, order, ct);
        db.StockMovements.AddRange(order.Cancel(products, user.UserId()));
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // vendedor só enxerga os próprios pedidos
    private static IQueryable<SalesOrder> VisibleOrders(AppDbContext db, ClaimsPrincipal user)
    {
        if (user.HasRole(Role.Admin))
            return db.SalesOrders;

        var userId = user.UserId();
        return db.SalesOrders.Where(o => o.SellerId == userId);
    }

    private static async Task<Dictionary<Guid, Product>> LoadProducts(AppDbContext db, SalesOrder order, CancellationToken ct)
    {
        var ids = order.Items.Select(i => i.ProductId).ToList();
        return await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
    }

    private static OrderDto ToDto(SalesOrder o) => new(
        o.Id, o.Number, o.CustomerName, o.CustomerDocument, o.Notes, o.Status.ToString(), o.Total, o.SellerId,
        o.Seller?.Name ?? "", o.CreatedAt, o.ConfirmedAt, o.CancelledAt,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.Sku, i.ProductName, i.Quantity, i.UnitPrice, i.Total)));
}
