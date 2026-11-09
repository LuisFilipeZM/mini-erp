using MiniErp.Api.Domain;

namespace MiniErp.Tests;

public class SalesOrderTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static Product NewProduct(string name, int stock, decimal cost, decimal price)
    {
        var p = new Product { Name = name, Sku = name.ToUpper(), SalePrice = price };
        p.Entry(stock, cost, MovementOrigin.Manual, UserId);
        return p;
    }

    [Fact]
    public void AddItem_calculates_total_and_merges_same_product()
    {
        var a = NewProduct("a", 10, 5m, 10m);
        var b = NewProduct("b", 10, 1m, 2.5m);
        var order = new SalesOrder();

        order.AddItem(a, 2);
        order.AddItem(b, 4);
        order.AddItem(a, 1);

        Assert.Equal(2, order.Items.Count);
        Assert.Equal(40m, order.Total);
    }

    [Fact]
    public void AddItem_rejects_inactive_product()
    {
        var p = NewProduct("a", 10, 5m, 10m);
        p.Active = false;

        Assert.Throws<DomainException>(() => new SalesOrder().AddItem(p, 1));
    }

    [Fact]
    public void Confirm_deducts_stock_and_generates_sale_movements()
    {
        var a = NewProduct("a", 10, 5m, 10m);
        var order = new SalesOrder();
        order.AddItem(a, 3);

        var movements = order.Confirm(new Dictionary<Guid, Product> { [a.Id] = a }, UserId).ToList();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(7, a.Stock);
        var m = Assert.Single(movements);
        Assert.Equal(MovementOrigin.Sale, m.Origin);
        Assert.Equal(order.Id, m.SalesOrderId);
        Assert.Equal(5m, order.Items.Single().UnitCost);
    }

    [Fact]
    public void Confirm_without_enough_stock_throws()
    {
        var a = NewProduct("a", 2, 5m, 10m);
        var order = new SalesOrder();
        order.AddItem(a, 5);

        Assert.Throws<DomainException>(() => order.Confirm(new Dictionary<Guid, Product> { [a.Id] = a }, UserId).ToList());
        Assert.Equal(OrderStatus.Open, order.Status);
    }

    [Fact]
    public void Cancel_confirmed_order_returns_stock()
    {
        var a = NewProduct("a", 10, 5m, 10m);
        var products = new Dictionary<Guid, Product> { [a.Id] = a };
        var order = new SalesOrder();
        order.AddItem(a, 4);
        order.Confirm(products, UserId);

        var movements = order.Cancel(products, UserId).ToList();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, a.Stock);
        Assert.Equal(MovementOrigin.SaleCancellation, Assert.Single(movements).Origin);
    }

    [Fact]
    public void Cancel_open_order_does_not_touch_stock()
    {
        var a = NewProduct("a", 10, 5m, 10m);
        var order = new SalesOrder();
        order.AddItem(a, 4);

        var movements = order.Cancel(new Dictionary<Guid, Product> { [a.Id] = a }, UserId);

        Assert.Empty(movements);
        Assert.Equal(10, a.Stock);
    }

    [Fact]
    public void Cannot_confirm_cancelled_order()
    {
        var a = NewProduct("a", 10, 5m, 10m);
        var products = new Dictionary<Guid, Product> { [a.Id] = a };
        var order = new SalesOrder();
        order.AddItem(a, 1);
        order.Cancel(products, UserId);

        Assert.Throws<DomainException>(() => order.Confirm(products, UserId));
    }
}
