using Microsoft.EntityFrameworkCore;

Console.WriteLine("Магазин спичек «Чирк»: проект запущен!");

using var db = new ShopContext();
db.Database.EnsureDeleted();
db.Database.EnsureCreated();