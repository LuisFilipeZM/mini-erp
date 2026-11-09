using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> hasher)
    {
        if (await db.Users.AnyAsync())
            return;

        var admin = NewUser("Administrador", "admin@minierp.dev", "Admin@123", Role.Admin, hasher);
        var seller = NewUser("Carla Vendas", "vendedor@minierp.dev", "Vendedor@123", Role.Seller, hasher);
        var keeper = NewUser("Rafael Estoque", "estoque@minierp.dev", "Estoque@123", Role.StockKeeper, hasher);
        db.Users.AddRange(admin, seller, keeper);

        var start = DateTime.UtcNow.Date.AddDays(-75);
        var catalog = new (string sku, string name, string cat, decimal cost, decimal price, int stock, int min)[]
        {
            ("PAR-001", "Parafuso sextavado 1/4 x 2\"", "Fixação", 0.18m, 0.45m, 2000, 300),
            ("PAR-002", "Porca sextavada 1/4", "Fixação", 0.07m, 0.20m, 2500, 400),
            ("BUC-008", "Bucha nylon 8mm (pct 100)", "Fixação", 6.90m, 14.90m, 120, 20),
            ("FER-010", "Martelo unha 27mm", "Ferramentas", 22.40m, 44.90m, 40, 8),
            ("FER-011", "Chave de fenda 1/4 x 6\"", "Ferramentas", 7.80m, 16.50m, 60, 10),
            ("FER-012", "Trena 5m emborrachada", "Ferramentas", 11.20m, 24.90m, 50, 10),
            ("FER-020", "Furadeira de impacto 650W", "Ferramentas", 189.00m, 329.90m, 8, 3),
            ("ELE-100", "Fita isolante 20m", "Elétrica", 3.10m, 7.90m, 200, 40),
            ("ELE-101", "Cabo flexível 2,5mm (rolo 100m)", "Elétrica", 165.00m, 259.00m, 10, 5),
            ("ELE-102", "Tomada 2P+T 10A", "Elétrica", 5.40m, 12.90m, 150, 30),
            ("ELE-103", "Disjuntor DIN 20A", "Elétrica", 9.80m, 21.50m, 80, 15),
            ("HID-200", "Fita veda rosca 18mm x 25m", "Hidráulica", 2.20m, 5.90m, 180, 30),
            ("HID-201", "Registro de gaveta 3/4", "Hidráulica", 38.00m, 69.90m, 20, 5),
            ("HID-202", "Joelho PVC 25mm 90°", "Hidráulica", 0.95m, 2.40m, 400, 80),
            ("PIN-300", "Tinta acrílica branca 18L", "Pintura", 210.00m, 349.00m, 9, 4),
            ("PIN-301", "Rolo de lã 23cm", "Pintura", 14.50m, 29.90m, 35, 8),
            ("PIN-302", "Lixa d'água grão 220", "Pintura", 1.10m, 2.90m, 300, 50),
        };

        var products = new List<Product>();
        foreach (var c in catalog)
        {
            var p = new Product
            {
                Sku = c.sku, Name = c.name, Category = c.cat, SalePrice = c.price, MinStock = c.min, CreatedAt = start
            };
            db.StockMovements.Add(p.Entry(c.stock, c.cost, MovementOrigin.Manual, keeper.Id, "Estoque inicial", at: start));
            products.Add(p);
        }
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        // histórico de vendas para o relatório de giro e o dashboard não ficarem vazios
        var rnd = new Random(42);
        string[] outOfSupplier = ["FER-020", "ELE-101", "PIN-300"];
        var customers = new[] { "Construtora Alfa", "João Pereira", "Mercado Bom Preço", "Ana Souza", "Oficina do Zé", "Condomínio Jardins" };

        for (var day = 70; day >= 1; day--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-day).AddHours(12);
            var ordersToday = rnd.Next(0, 3);

            for (var n = 0; n < ordersToday; n++)
            {
                var order = new SalesOrder
                {
                    CustomerName = customers[rnd.Next(customers.Length)],
                    SellerId = seller.Id,
                    CreatedAt = date.AddMinutes(n * 37)
                };

                foreach (var p in products.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 4)))
                {
                    var qty = Math.Max(1, p.MinStock / rnd.Next(4, 12));
                    if (p.Stock - qty > 0)
                        order.AddItem(p, qty);
                }

                if (order.Items.Count == 0)
                    continue;

                db.SalesOrders.Add(order);
                await db.SaveChangesAsync();
                db.StockMovements.AddRange(order.Confirm(products.ToDictionary(p => p.Id), seller.Id, order.CreatedAt.AddMinutes(5)));
            }

            // reposição quando cai abaixo do mínimo
            if (day % 10 == 0)
            {
                foreach (var p in products.Where(p => p.Stock <= p.MinStock * 1.5 && !outOfSupplier.Contains(p.Sku)))
                    db.StockMovements.Add(p.Entry(p.MinStock * 3, Math.Round(p.CostPrice * 1.03m, 2),
                        MovementOrigin.Manual, keeper.Id, "Reposição de fornecedor", at: date.AddHours(-3)));
            }

            await db.SaveChangesAsync();
        }

        db.SalesOrders.Add(OpenOrder(products, seller.Id, "Pedro Almeida", 0, 4, 2));
        db.SalesOrders.Add(OpenOrder(products, admin.Id, "Construtora Alfa", 6, 7, 1));
        await db.SaveChangesAsync();
    }

    private static SalesOrder OpenOrder(List<Product> products, Guid sellerId, string customer, int a, int b, int qty)
    {
        var order = new SalesOrder { CustomerName = customer, SellerId = sellerId };
        order.AddItem(products[a], qty * 10);
        order.AddItem(products[b], qty);
        return order;
    }

    private static User NewUser(string name, string email, string password, Role role, IPasswordHasher<User> hasher)
    {
        var user = new User { Name = name, Email = email, Role = role };
        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }
}
