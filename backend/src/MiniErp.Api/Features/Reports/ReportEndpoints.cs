using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Common;
using MiniErp.Api.Data;

namespace MiniErp.Api.Features.Reports;

public static class ReportEndpoints
{
    public record TurnoverRow(
        Guid ProductId, string Sku, string Name, string? Category, int OpeningStock, int ClosingStock,
        decimal AverageStock, int QuantitySold, decimal CostOfGoodsSold, decimal Turnover, decimal? CoverageDays);

    public record TurnoverReport(
        DateOnly From, DateOnly To, int Days, decimal TotalCostOfGoodsSold, decimal OverallTurnover,
        PagedResult<TurnoverRow> Rows);

    private record RawRow(Guid ProductId, string Sku, string Name, string? Category, int OpeningStock,
        int ClosingStock, int QuantitySold, decimal CostOfGoodsSold, decimal CurrentCost);

    public static void MapReports(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization(Policies.ViewReports);
        group.MapGet("/stock-turnover", StockTurnover);
    }

    private static async Task<IResult> StockTurnover(AppDbContext db, DateOnly? from, DateOnly? to, string? category,
        string? search, string? sort, int? page, int? pageSize, CancellationToken ct)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-29);

        if (start > end)
            return Results.Problem(statusCode: 400, title: "Período inválido", detail: "A data inicial deve ser anterior à final.");

        var startUtc = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var days = end.DayNumber - start.DayNumber + 1;

        // saldo de abertura/fechamento = balance_after do último movimento antes de cada data
        var raw = await db.Database.SqlQuery<RawRow>($"""
            WITH opening AS (
                SELECT DISTINCT ON (product_id) product_id, balance_after
                FROM stock_movements
                WHERE created_at < {startUtc}
                ORDER BY product_id, created_at DESC, id DESC
            ),
            closing AS (
                SELECT DISTINCT ON (product_id) product_id, balance_after
                FROM stock_movements
                WHERE created_at < {endUtc}
                ORDER BY product_id, created_at DESC, id DESC
            ),
            sold AS (
                SELECT product_id,
                       SUM(CASE WHEN origin = 'Sale' THEN quantity ELSE -quantity END) AS qty,
                       SUM(CASE WHEN origin = 'Sale' THEN quantity * unit_cost ELSE -quantity * unit_cost END) AS cost
                FROM stock_movements
                WHERE origin IN ('Sale', 'SaleCancellation')
                  AND created_at >= {startUtc} AND created_at < {endUtc}
                GROUP BY product_id
            )
            SELECT p.id AS product_id,
                   p.sku,
                   p.name,
                   p.category,
                   COALESCE(o.balance_after, 0) AS opening_stock,
                   COALESCE(c.balance_after, 0) AS closing_stock,
                   COALESCE(s.qty, 0)::int AS quantity_sold,
                   COALESCE(s.cost, 0) AS cost_of_goods_sold,
                   p.cost_price AS current_cost
            FROM products p
            LEFT JOIN opening o ON o.product_id = p.id
            LEFT JOIN closing c ON c.product_id = p.id
            LEFT JOIN sold s ON s.product_id = p.id
            WHERE p.created_at < {endUtc}
            """).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(category))
            raw = raw.Where(r => r.Category == category).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            raw = raw.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                 r.Sku.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        var rows = raw.Select(r =>
        {
            var avg = (r.OpeningStock + r.ClosingStock) / 2m;
            var turnover = avg > 0 ? Math.Round(r.QuantitySold / avg, 2) : 0;
            decimal? coverage = r.QuantitySold > 0 ? Math.Round(r.ClosingStock / (r.QuantitySold / (decimal)days), 1) : null;
            return new TurnoverRow(r.ProductId, r.Sku, r.Name, r.Category, r.OpeningStock, r.ClosingStock, avg,
                r.QuantitySold, Math.Round(r.CostOfGoodsSold, 2), turnover, coverage);
        });

        rows = sort switch
        {
            "turnover" => rows.OrderBy(r => r.Turnover),
            "sold_desc" => rows.OrderByDescending(r => r.QuantitySold),
            "cogs_desc" => rows.OrderByDescending(r => r.CostOfGoodsSold),
            "name" => rows.OrderBy(r => r.Name),
            _ => rows.OrderByDescending(r => r.Turnover).ThenBy(r => r.Name)
        };

        var list = rows.ToList();
        var totalCogs = list.Sum(r => r.CostOfGoodsSold);

        // giro geral em valor: CMV / estoque médio a custo (aproximado pelo custo atual)
        var avgStockValue = raw.Sum(r => (r.OpeningStock + r.ClosingStock) / 2m * r.CurrentCost);
        var overall = avgStockValue > 0 ? Math.Round(totalCogs / avgStockValue, 2) : 0;

        return Results.Ok(new TurnoverReport(start, end, days, totalCogs, overall, list.ToPaged(page, pageSize)));
    }
}
