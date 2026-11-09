namespace MiniErp.Api.Domain;

public enum MovementType
{
    In,
    Out
}

public enum MovementOrigin
{
    Manual,
    Sale,
    SaleCancellation
}

public class StockMovement
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ProductId { get; init; }
    public Product? Product { get; init; }
    public MovementType Type { get; init; }
    public MovementOrigin Origin { get; init; }
    public int Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public int BalanceAfter { get; init; }
    public string? Note { get; init; }
    public Guid? SalesOrderId { get; init; }
    public Guid UserId { get; init; }
    public User? User { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
