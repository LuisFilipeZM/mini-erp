using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Common;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public record DailySales(DateOnly Date, decimal Total, int Orders);
    public record LowStockItem(Guid Id, string Sku, string Name, int Stock, int MinStock);

    public record DashboardDto(
        int ActiveProducts, int BelowMinimum, decimal StockValue, int OpenOrders,
        decimal SalesThisMonth, int OrdersThisMonth, IEnumerable<DailySales> Last14Days,
        IEnumerable<LowStockItem> LowStock);

    public static void MapDashboard(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/dashboard", Get).WithTags("Dashboard").RequireAuthorization();

    private static async Task<DashboardDto> Get(AppDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var since = now.Date.AddDays(-13);

        var products = db.Products.AsNoTracking().Where(p => p.Active);
        var orders = db.SalesOrders.AsNoTracking();

        if (user.HasRole(Role.Seller))
        {
            var sellerId = user.UserId();
            orders = orders.Where(o => o.SellerId == sellerId);
        }

        var activeProducts = await products.CountAsync(ct);
        var belowMinimum = await products.CountAsync(p => p.Stock <= p.MinStock, ct);
        var stockValue = await products.SumAsync(p => p.Stock * p.CostPrice, ct);
        var openOrders = await orders.CountAsync(o => o.Status == OrderStatus.Open, ct);

        var confirmed = orders.Where(o => o.Status == OrderStatus.Confirmed);
        var salesMonth = await confirmed.Where(o => o.ConfirmedAt >= monthStart).SumAsync(o => o.Total, ct);
        var ordersMonth = await confirmed.CountAsync(o => o.ConfirmedAt >= monthStart, ct);

        var daily = await confirmed
            .Where(o => o.ConfirmedAt >= since)
            .GroupBy(o => o.ConfirmedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.Total), Count = g.Count() })
            .ToListAsync(ct);

        var series = Enumerable.Range(0, 14)
            .Select(i => since.AddDays(i))
            .Select(d =>
            {
                var hit = daily.FirstOrDefault(x => x.Date == d);
                return new DailySales(DateOnly.FromDateTime(d), hit?.Total ?? 0, hit?.Count ?? 0);
            });

        var lowStock = await products
            .Where(p => p.Stock <= p.MinStock)
            .OrderBy(p => p.Stock - p.MinStock)
            .Take(8)
            .Select(p => new LowStockItem(p.Id, p.Sku, p.Name, p.Stock, p.MinStock))
            .ToListAsync(ct);

        return new DashboardDto(activeProducts, belowMinimum, stockValue, openOrders, salesMonth, ordersMonth,
            series.ToList(), lowStock);
    }
}
