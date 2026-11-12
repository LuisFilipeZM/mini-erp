using MiniErp.Api.Domain;
using MiniErp.Api.Features.Orders;
using MiniErp.Api.Features.Products;
using MiniErp.Api.Features.Stock;

namespace MiniErp.Tests;

public class ValidatorTests
{
    [Fact]
    public void Product_with_invalid_sku_and_negative_price_is_rejected()
    {
        var validator = new ProductEndpoints.CreateValidator();
        var req = new ProductEndpoints.CreateProductRequest("SKU COM ESPAÇO", "Produto", null, null, null, -1, 0, 0, 0);

        var result = validator.Validate(req);

        Assert.Contains(result.Errors, e => e.PropertyName == "Sku");
        Assert.Contains(result.Errors, e => e.PropertyName == "SalePrice");
    }

    [Fact]
    public void Stock_entry_requires_unit_cost()
    {
        var validator = new StockEndpoints.RegisterValidator();

        var entry = validator.Validate(new StockEndpoints.RegisterMovementRequest(Guid.NewGuid(), MovementType.In, 5, null, null));
        var exit = validator.Validate(new StockEndpoints.RegisterMovementRequest(Guid.NewGuid(), MovementType.Out, 5, null, null));

        Assert.False(entry.IsValid);
        Assert.True(exit.IsValid);
    }

    [Fact]
    public void Order_without_items_is_rejected()
    {
        var validator = new OrderEndpoints.CreateValidator();

        var result = validator.Validate(new OrderEndpoints.CreateOrderRequest("Cliente", null, null, []));

        Assert.Contains(result.Errors, e => e.PropertyName == "Items");
    }
}
