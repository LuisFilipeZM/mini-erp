namespace MiniErp.Api.Domain;

public enum OrderStatus
{
    Open,
    Confirmed,
    Cancelled
}

public class SalesOrder
{
    private readonly List<SalesOrderItem> _items = [];

    public Guid Id { get; init; } = Guid.NewGuid();
    public int Number { get; private set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerDocument { get; set; }
    public string? Notes { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Open;
    public decimal Total { get; private set; }
    public Guid SellerId { get; init; }
    public User? Seller { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public IReadOnlyCollection<SalesOrderItem> Items => _items;

    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Open)
            throw new DomainException("Só é possível alterar pedidos em aberto.");
        if (!product.Active)
            throw new DomainException($"O produto \"{product.Name}\" está inativo.");
        if (quantity <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _items.Add(new SalesOrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Sku = product.Sku,
                Quantity = quantity,
                UnitPrice = product.SalePrice
            });
        }

        Total = _items.Sum(i => i.Total);
    }

    public IEnumerable<StockMovement> Confirm(IReadOnlyDictionary<Guid, Product> products, Guid userId, DateTime? at = null)
    {
        if (Status != OrderStatus.Open)
            throw new DomainException("Apenas pedidos em aberto podem ser confirmados.");
        if (_items.Count == 0)
            throw new DomainException("O pedido não possui itens.");

        var when = at ?? DateTime.UtcNow;
        var movements = new List<StockMovement>();

        foreach (var item in _items)
        {
            var product = products[item.ProductId];
            item.UnitCost = product.CostPrice;
            movements.Add(product.Exit(item.Quantity, MovementOrigin.Sale, userId, $"Pedido #{Number}", Id, when));
        }

        Status = OrderStatus.Confirmed;
        ConfirmedAt = when;
        return movements;
    }

    public IEnumerable<StockMovement> Cancel(IReadOnlyDictionary<Guid, Product> products, Guid userId)
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("O pedido já está cancelado.");

        var movements = new List<StockMovement>();

        if (Status == OrderStatus.Confirmed)
        {
            foreach (var item in _items)
            {
                var product = products[item.ProductId];
                movements.Add(product.Entry(item.Quantity, item.UnitCost, MovementOrigin.SaleCancellation, userId,
                    $"Cancelamento do pedido #{Number}", Id));
            }
        }

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        return movements;
    }
}

public class SalesOrderItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SalesOrderId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public string Sku { get; init; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; init; }
    public decimal UnitCost { get; set; }

    public decimal Total => Quantity * UnitPrice;
}
