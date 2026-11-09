using MiniErp.Api.Domain;

namespace MiniErp.Tests;

public class ProductTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Entry_increases_stock_and_records_balance()
    {
        var product = new Product { Name = "Martelo" };

        var movement = product.Entry(10, 20m, MovementOrigin.Manual, UserId);

        Assert.Equal(10, product.Stock);
        Assert.Equal(MovementType.In, movement.Type);
        Assert.Equal(10, movement.BalanceAfter);
    }

    [Fact]
    public void Entry_recalculates_weighted_average_cost()
    {
        var product = new Product { Name = "Martelo" };
        product.Entry(10, 20m, MovementOrigin.Manual, UserId);

        product.Entry(10, 30m, MovementOrigin.Manual, UserId);

        Assert.Equal(25m, product.CostPrice);
    }

    [Fact]
    public void Exit_uses_current_cost_and_decreases_stock()
    {
        var product = new Product { Name = "Trena" };
        product.Entry(5, 12m, MovementOrigin.Manual, UserId);

        var movement = product.Exit(3, MovementOrigin.Sale, UserId);

        Assert.Equal(2, product.Stock);
        Assert.Equal(12m, movement.UnitCost);
        Assert.Equal(2, movement.BalanceAfter);
    }

    [Fact]
    public void Exit_more_than_available_throws()
    {
        var product = new Product { Name = "Trena" };
        product.Entry(2, 12m, MovementOrigin.Manual, UserId);

        var ex = Assert.Throws<DomainException>(() => product.Exit(3, MovementOrigin.Manual, UserId));

        Assert.Contains("Estoque insuficiente", ex.Message);
        Assert.Equal(2, product.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Invalid_quantity_throws(int qty)
    {
        var product = new Product { Name = "Trena" };

        Assert.Throws<DomainException>(() => product.Entry(qty, 1m, MovementOrigin.Manual, UserId));
        Assert.Throws<DomainException>(() => product.Exit(qty, MovementOrigin.Manual, UserId));
    }

    [Fact]
    public void Initial_cost_cannot_be_set_after_stock_entry()
    {
        var product = new Product { Name = "Trena" };
        product.Entry(1, 10m, MovementOrigin.Manual, UserId);

        Assert.Throws<DomainException>(() => product.SetInitialCost(5m));
    }
}
