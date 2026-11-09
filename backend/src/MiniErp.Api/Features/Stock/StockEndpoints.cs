using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Common;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Features.Stock;

public static class StockEndpoints
{
    public record MovementDto(
        Guid Id, Guid ProductId, string Sku, string ProductName, string Type, string Origin, int Quantity,
        decimal UnitCost, int BalanceAfter, string? Note, Guid? SalesOrderId, string UserName, DateTime CreatedAt);

    public record RegisterMovementRequest(Guid ProductId, MovementType Type, int Quantity, decimal? UnitCost, string? Note);

    public class RegisterValidator : AbstractValidator<RegisterMovementRequest>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithName("Produto");
            RuleFor(x => x.Type).IsInEnum().WithName("Tipo");
            RuleFor(x => x.Quantity).GreaterThan(0).WithName("Quantidade");
            RuleFor(x => x.UnitCost).NotNull().GreaterThanOrEqualTo(0)
                .When(x => x.Type == MovementType.In)
                .WithMessage("Informe o custo unitário da entrada.").WithName("Custo unitário");
            RuleFor(x => x.Note).MaximumLength(300).WithName("Observação");
        }
    }

    public static void MapStock(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock").WithTags("Stock").RequireAuthorization(Policies.ManageStock);

        group.MapGet("/movements", List);
        group.MapPost("/movements", Register).Validate<RegisterMovementRequest>();
    }

    private static async Task<PagedResult<MovementDto>> List(AppDbContext db, Guid? productId, MovementType? type,
        MovementOrigin? origin, DateOnly? from, DateOnly? to, int? page, int? pageSize, CancellationToken ct)
    {
        var query = db.StockMovements.AsNoTracking();

        if (productId is not null)
            query = query.Where(m => m.ProductId == productId);
        if (type is not null)
            query = query.Where(m => m.Type == type);
        if (origin is not null)
            query = query.Where(m => m.Origin == origin);
        if (from is not null)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.CreatedAt >= start);
        }
        if (to is not null)
        {
            var end = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.CreatedAt < end);
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MovementDto(m.Id, m.ProductId, m.Product!.Sku, m.Product.Name, m.Type.ToString(),
                m.Origin.ToString(), m.Quantity, m.UnitCost, m.BalanceAfter, m.Note, m.SalesOrderId, m.User!.Name,
                m.CreatedAt))
            .ToPagedAsync(page, pageSize, ct);
    }

    private static async Task<IResult> Register(RegisterMovementRequest req, AppDbContext db, ClaimsPrincipal user,
        CancellationToken ct)
    {
        var product = await db.Products.FindAsync([req.ProductId], ct);
        if (product is null)
            return Results.Problem(statusCode: 404, title: "Produto não encontrado");

        var movement = req.Type == MovementType.In
            ? product.Entry(req.Quantity, req.UnitCost!.Value, MovementOrigin.Manual, user.UserId(), req.Note)
            : product.Exit(req.Quantity, MovementOrigin.Manual, user.UserId(), req.Note);

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/stock/movements/{movement.Id}", new
        {
            movement.Id,
            ProductStock = product.Stock,
            ProductCost = product.CostPrice
        });
    }
}
