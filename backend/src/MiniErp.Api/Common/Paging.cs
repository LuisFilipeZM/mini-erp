using Microsoft.EntityFrameworkCore;

namespace MiniErp.Api.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    public const int MaxPageSize = 100;

    public static (int page, int size) Normalize(int? page, int? pageSize)
    {
        var p = page is null or < 1 ? 1 : page.Value;
        var s = pageSize is null or < 1 ? 20 : Math.Min(pageSize.Value, MaxPageSize);
        return (p, s);
    }

    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, int? page, int? pageSize,
        CancellationToken ct)
    {
        var (p, s) = Normalize(page, pageSize);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((p - 1) * s).Take(s).ToListAsync(ct);
        return new PagedResult<T>(items, p, s, total);
    }

    public static PagedResult<T> ToPaged<T>(this IEnumerable<T> source, int? page, int? pageSize)
    {
        var (p, s) = Normalize(page, pageSize);
        var list = source as IList<T> ?? source.ToList();
        return new PagedResult<T>(list.Skip((p - 1) * s).Take(s).ToList(), p, s, list.Count);
    }
}
