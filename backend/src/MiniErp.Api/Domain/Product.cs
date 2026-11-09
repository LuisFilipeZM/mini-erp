namespace MiniErp.Api.Domain;

public class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "UN";
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; private set; }
    public int Stock { get; private set; }
    public int MinStock { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public uint Version { get; private set; }

    public bool IsBelowMinimum => Stock <= MinStock;

    public void SetInitialCost(decimal cost)
    {
        if (Stock > 0)
            throw new DomainException("O custo só pode ser definido diretamente antes da primeira entrada.");
        CostPrice = cost;
    }

    public StockMovement Entry(int quantity, decimal unitCost, MovementOrigin origin, Guid userId,
        string? note = null, Guid? orderId = null, DateTime? at = null)
    {
        if (quantity <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");
        if (unitCost < 0)
            throw new DomainException("O custo unitário não pode ser negativo.");

        var newStock = Stock + quantity;
        CostPrice = Math.Round((Stock * CostPrice + quantity * unitCost) / newStock, 4);
        Stock = newStock;

        return NewMovement(MovementType.In, origin, quantity, unitCost, userId, note, orderId, at);
    }

    public StockMovement Exit(int quantity, MovementOrigin origin, Guid userId,
        string? note = null, Guid? orderId = null, DateTime? at = null)
    {
        if (quantity <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");
        if (quantity > Stock)
            throw new DomainException($"Estoque insuficiente para \"{Name}\". Disponível: {Stock}, solicitado: {quantity}.");

        Stock -= quantity;

        return NewMovement(MovementType.Out, origin, quantity, CostPrice, userId, note, orderId, at);
    }

    private StockMovement NewMovement(MovementType type, MovementOrigin origin, int quantity, decimal unitCost,
        Guid userId, string? note, Guid? orderId, DateTime? at) => new()
    {
        ProductId = Id,
        Type = type,
        Origin = origin,
        Quantity = quantity,
        UnitCost = unitCost,
        BalanceAfter = Stock,
        UserId = userId,
        Note = note,
        SalesOrderId = orderId,
        CreatedAt = at ?? DateTime.UtcNow
    };
}
