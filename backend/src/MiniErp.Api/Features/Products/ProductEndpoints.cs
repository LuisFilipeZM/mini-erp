using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Common;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Features.Products;

public static class ProductEndpoints
{
    public record ProductDto(
        Guid Id, string Sku, string Name, string? Description, string? Category, string Unit,
        decimal SalePrice, decimal CostPrice, int Stock, int MinStock, bool Active, bool BelowMinimum,
        DateTime CreatedAt, DateTime? UpdatedAt);

    public record CreateProductRequest(
        string Sku, string Name, string? Description, string? Category, string? Unit,
        decimal SalePrice, decimal CostPrice, int MinStock, int InitialStock);

    public record UpdateProductRequest(
        string Sku, string Name, string? Description, string? Category, string? Unit,
        decimal SalePrice, int MinStock, bool Active);

    public class CreateValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateValidator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(40)
                .Matches("^[A-Za-z0-9-_.]+$").WithMessage("O SKU deve conter apenas letras, números, '-', '_' ou '.'.").WithName("SKU");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithName("Nome");
            RuleFor(x => x.Description).MaximumLength(1000).WithName("Descrição");
            RuleFor(x => x.Category).MaximumLength(60).WithName("Categoria");
            RuleFor(x => x.Unit).MaximumLength(6).WithName("Unidade");
            RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).WithName("Preço de venda");
            RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0).WithName("Custo");
            RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0).WithName("Estoque mínimo");
            RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0).WithName("Estoque inicial");
        }
    }

    public class UpdateValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateValidator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(40)
                .Matches("^[A-Za-z0-9-_.]+$").WithMessage("O SKU deve conter apenas letras, números, '-', '_' ou '.'.").WithName("SKU");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150).WithName("Nome");
            RuleFor(x => x.Description).MaximumLength(1000).WithName("Descrição");
            RuleFor(x => x.Category).MaximumLength(60).WithName("Categoria");
            RuleFor(x => x.Unit).MaximumLength(6).WithName("Unidade");
            RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).WithName("Preço de venda");
            RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0).WithName("Estoque mínimo");
        }
    }

    public static void MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products").RequireAuthorization();

        group.MapGet("/", List);
        group.MapGet("/categories", Categories);
        group.MapGet("/{id:guid}", Get);
        group.MapPost("/", Create).Validate<CreateProductRequest>().RequireAuthorization(Policies.ManageStock);
        group.MapPut("/{id:guid}", Update).Validate<UpdateProductRequest>().RequireAuthorization(Policies.ManageStock);
    }

    private static async Task<PagedResult<ProductDto>> List(AppDbContext db, string? search, string? category,
        bool? active, bool? belowMinimum, string? sort, int? page, int? pageSize, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.Sku, term));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (active is not null)
            query = query.Where(p => p.Active == active);

        if (belowMinimum == true)
            query = query.Where(p => p.Stock <= p.MinStock);

        query = sort switch
        {
            "name_desc" => query.OrderByDescending(p => p.Name),
            "stock" => query.OrderBy(p => p.Stock).ThenBy(p => p.Name),
            "stock_desc" => query.OrderByDescending(p => p.Stock).ThenBy(p => p.Name),
            "price" => query.OrderBy(p => p.SalePrice),
            "price_desc" => query.OrderByDescending(p => p.SalePrice),
            "recent" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name)
        };

        return await query.Select(ToDto).ToPagedAsync(page, pageSize, ct);
    }

    private static async Task<List<string>> Categories(AppDbContext db, CancellationToken ct) =>
        await db.Products.AsNoTracking()
            .Where(p => p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

    private static async Task<IResult> Get(Guid id, AppDbContext db, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().Where(p => p.Id == id).Select(ToDto).FirstOrDefaultAsync(ct);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> Create(CreateProductRequest req, AppDbContext db, ClaimsPrincipal user,
        CancellationToken ct)
    {
        var sku = req.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(p => p.Sku == sku, ct))
            return Results.Problem(statusCode: 409, title: "Conflito", detail: $"O SKU {sku} já está cadastrado.");

        var product = new Product
        {
            Sku = sku,
            Name = req.Name.Trim(),
            Description = req.Description,
            Category = Clean(req.Category),
            Unit = string.IsNullOrWhiteSpace(req.Unit) ? "UN" : req.Unit.Trim().ToUpperInvariant(),
            SalePrice = req.SalePrice,
            MinStock = req.MinStock
        };

        db.Products.Add(product);

        if (req.InitialStock > 0)
            db.StockMovements.Add(product.Entry(req.InitialStock, req.CostPrice, MovementOrigin.Manual, user.UserId(), "Estoque inicial"));
        else
            product.SetInitialCost(req.CostPrice);

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/products/{product.Id}", ToDto.Compile()(product));
    }

    private static async Task<IResult> Update(Guid id, UpdateProductRequest req, AppDbContext db, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return Results.NotFound();

        var sku = req.Sku.Trim().ToUpperInvariant();
        if (sku != product.Sku && await db.Products.AnyAsync(p => p.Sku == sku, ct))
            return Results.Problem(statusCode: 409, title: "Conflito", detail: $"O SKU {sku} já está cadastrado.");

        product.Sku = sku;
        product.Name = req.Name.Trim();
        product.Description = req.Description;
        product.Category = Clean(req.Category);
        product.Unit = string.IsNullOrWhiteSpace(req.Unit) ? "UN" : req.Unit.Trim().ToUpperInvariant();
        product.SalePrice = req.SalePrice;
        product.MinStock = req.MinStock;
        product.Active = req.Active;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly System.Linq.Expressions.Expression<Func<Product, ProductDto>> ToDto = p => new ProductDto(
        p.Id, p.Sku, p.Name, p.Description, p.Category, p.Unit, p.SalePrice, p.CostPrice, p.Stock, p.MinStock,
        p.Active, p.Stock <= p.MinStock, p.CreatedAt, p.UpdatedAt);
}
