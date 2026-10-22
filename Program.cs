using Microsoft.EntityFrameworkCore;

Console.WriteLine("Магазин спичек «Чирк»: проект запущен!");

using var db = new ShopContext();
db.Database.EnsureDeleted();
db.Database.EnsureCreated();

Console.WriteLine("=== Шаг 3 ===");

var ivan = new Customer { Name = "Иван Петров", Phone = "+7 900 111-22-33", City = "Омск" };
var olga = new Customer { Name = "Ольга Смирнова", Phone = "+7 900 444-55-66", City = "Томск" };
var anna = new Customer { Name = "Анна Сидорова", Phone = "+7 900 777-88-99", City = "Москва" };
var dmitry = new Customer { Name = "Дмитрий Козлов", Phone = "+7 900 333-22-11", City = "Омск" };
var elena = new Customer { Name = "Елена Морозова", Phone = "+7 900 555-66-77", City = "Казань" };

db.Customers.AddRange(ivan, olga, anna, dmitry, elena);

var p1 = new Product { Name = "Бытовые", Price = 30m, MatchesPerBox = 40, StockQuantity = 200, Description = "Обычные спички" };
var p2 = new Product { Name = "Хозяйственные", Price = 80m, MatchesPerBox = 100, StockQuantity = 150 };
var p3 = new Product { Name = "Каминные", Price = 150m, MatchesPerBox = 50, StockQuantity = 80, Description = "Длинные" };
var p4 = new Product { Name = "Штормовые", Price = 250m, MatchesPerBox = 20, StockQuantity = 50, Description = "Горят даже под дождём" };
var p5 = new Product { Name = "Охотничьи", Price = 180m, MatchesPerBox = 30, StockQuantity = 70 };

db.Products.AddRange(p1, p2, p3, p4, p5);

var orders = new List<Order>
{
    new() { Customer = ivan, Product = p3, Quantity = 2, CreatedAt = DateTime.UtcNow },
    new() { Customer = ivan, Product = p1, Quantity = 5, CreatedAt = DateTime.UtcNow },
    new() { Customer = anna, Product = p4, Quantity = 1, CreatedAt = DateTime.UtcNow },
    new() { Customer = dmitry, Product = p2, Quantity = 3, CreatedAt = DateTime.UtcNow }
};

db.Orders.AddRange(orders);
db.SaveChanges();

Console.WriteLine("=== Шаг 4 ===");

var cheapProducts = db.Products.Where(p => p.Price < 100m).OrderBy(p => p.Price).ToList();
foreach (var p in cheapProducts)
    Console.WriteLine($"{p.Name} — {p.Price} ₽");

var inStockCount = db.Products.Count(p => p.StockQuantity > 0);
Console.WriteLine($"Видов на складе: {inStockCount}");

var expensiveProducts = db.Products.OrderByDescending(p => p.Price).Take(3).ToList();
foreach (var p in expensiveProducts)
    Console.WriteLine($"{p.Name} — {p.Price} ₽");

var allOrders = db.Orders.Include(o => o.Customer).Include(o => o.Product).ToList();
decimal totalRevenue = 0;
foreach (var o in allOrders)
{
    var sum = o.Product.Price * o.Quantity;
    totalRevenue += sum;
    Console.WriteLine($"{o.Customer.Name} — {o.Product.Name} — {o.Quantity} шт. — {sum} ₽");
}
Console.WriteLine($"Выручка: {totalRevenue} ₽");

Console.WriteLine("=== Шаг 5 ===");

var customer = db.Customers.First(c => c.Name == "Иван Петров");
customer.Phone = "+7 900 999-00-00";

foreach (var product in db.Products)
{
    product.Price *= 1.1m;
}

var zeroStockProduct = db.Products.First(p => p.Name == "Бытовые");
zeroStockProduct.StockQuantity = 0;

db.SaveChanges();

void MakeOrder(ShopContext context, Customer targetCustomer, Product targetProduct, int quantity)
{
    if (targetProduct.StockQuantity >= quantity)
    {
        var newOrder = new Order
        {
            Customer = targetCustomer,
            Product = targetProduct,
            Quantity = quantity,
            CreatedAt = DateTime.UtcNow
        };
        targetProduct.StockQuantity -= quantity;
        context.Orders.Add(newOrder);
        context.SaveChanges();
        Console.WriteLine($"Заказ оформлен: {targetCustomer.Name} — {targetProduct.Name} — {quantity} шт.");
    }
    else
    {
        Console.WriteLine("Недостаточно на складе");
    }
}

var annaCustomer = db.Customers.First(c => c.Name == "Анна Сидорова");
var stormProduct = db.Products.First(p => p.Name == "Штормовые");
MakeOrder(db, annaCustomer, stormProduct, 100);
MakeOrder(db, annaCustomer, stormProduct, 5);

Console.WriteLine("=== Шаг 6 ===");

var outOfStockProducts = db.Products.Where(p => p.StockQuantity == 0).ToList();
db.Products.RemoveRange(outOfStockProducts);
db.SaveChanges();

var remainingCount = db.Products.Count();
Console.WriteLine($"Осталось товаров: {remainingCount}");

var olgaCustomer = db.Customers.Include(c => c.Orders).First(c => c.Name == "Ольга Смирнова");
db.Customers.Remove(olgaCustomer);
db.SaveChanges();

/*
Шаг 1 - название не может быть null (NOT NULL), а описание может (NULL), так как в C# string обязательный, а string? опциональный
Шаг 2 - выбран тип DateTime, в PostgreSQL создался тип timestamp with time zone
Шаг 3 - DateTime.Now возвращает локальное время устройства, DateTime.UtcNow - всемирное время
Шаг 5 - чтобы гарантировать транзакционность. Изменения сохраняются атомарно, исключая рассинхронизацию остатков и заказов при сбое
Шаг 6 - заказы удалились каскадно. Для магазина это плохо, так как теряется история продаж и финансовая отчётность
*/