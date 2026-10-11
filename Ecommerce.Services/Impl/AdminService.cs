using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Ecommerce.Core.Common;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data;

namespace Ecommerce.Services.Impl
{
    /// <summary>
    /// Admin reporting. Metric rules (also displayed on the dashboard):
    /// qualifying orders exclude Status 'Cancelled'/'Refunded'; units sold =
    /// SUM(OrderLines.Quantity); recorded sales = SUM(OrderLines.LineTotal)
    /// (purchase-time prices, never current catalogue prices); pending =
    /// Status 'Placed'; completed = 'Shipped'/'Delivered'/'Completed';
    /// low stock = 0 &lt; Stock &lt; threshold; out of stock = Stock == 0.
    /// All aggregations run database-side.
    /// </summary>
    public class AdminService : IAdminService
    {
        private static readonly string[] ExcludedStatuses = { "Cancelled", "Refunded" };
        private static readonly string[] CompletedStatuses = { "Shipped", "Delivered", "Completed" };
        private readonly EcommerceDbContext _db;
        private readonly ICatalogService _catalog;
        public AdminService(EcommerceDbContext db, ICatalogService catalog) { _db = db; _catalog = catalog; }

        public AdminDashboardViewModel GetDashboard(int lowStockThreshold)
        {
            if (lowStockThreshold < 1) lowStockThreshold = 10;
            var vm = new AdminDashboardViewModel { LowStockThreshold = lowStockThreshold };
            vm.TotalProducts = _db.Products.Count();
            vm.ActiveProducts = _db.Products.Count(p => p.IsActive);
            vm.InactiveProducts = vm.TotalProducts - vm.ActiveProducts;
            vm.TotalCategories = _db.Categories.Count();
            vm.LowStockProducts = _db.Products.Count(p => p.StockQuantity > 0 && p.StockQuantity < lowStockThreshold);
            vm.OutOfStockProducts = _db.Products.Count(p => p.StockQuantity <= 0);
            vm.TotalOrders = _db.Orders.Count();
            vm.PendingOrders = _db.Orders.Count(o => o.Status == "Placed");
            vm.CompletedOrders = _db.Orders.Count(o => CompletedStatuses.Contains(o.Status));
            var qlines = _db.OrderLines.Where(l => !ExcludedStatuses.Contains(l.Order.Status));
            vm.TotalUnitsSold = qlines.Sum(l => (int?)l.Quantity) ?? 0;
            vm.TotalRecordedSales = qlines.Sum(l => (decimal?)l.LineTotal) ?? 0m;

            var recent = _db.Orders.OrderByDescending(o => o.CreatedUtc).Take(10)
                .Select(o => new { o.OrderNumber, o.CreatedUtc, o.GrandTotal, o.Status }).ToList();
            var recentIds = recent.Select(o => o.OrderNumber).ToList();
            var counts = _db.OrderLines.Where(l => recentIds.Contains(l.Order.OrderNumber))
                .GroupBy(l => l.Order.OrderNumber)
                .ToDictionary(g => g.Key, g => g.Sum(l => (int?)l.Quantity) ?? 0);
            foreach (var o in recent)
                vm.RecentOrders.Add(new OrderHistoryItemViewModel
                {
                    OrderNumber = o.OrderNumber, CreatedUtc = o.CreatedUtc,
                    GrandTotal = o.GrandTotal, Status = o.Status,
                    ItemCount = counts.ContainsKey(o.OrderNumber) ? counts[o.OrderNumber] : 0
                });

            var best = qlines.GroupBy(l => new { l.ProductId, l.ProductName })
                .Select(g => new { g.Key.ProductId, g.Key.ProductName, Units = g.Sum(l => (int?)l.Quantity) ?? 0, Revenue = g.Sum(l => (decimal?)l.LineTotal) ?? 0m })
                .OrderByDescending(x => x.Units).Take(8).ToList();
            var pids = best.Select(b => b.ProductId).ToList();
            var slugs = _db.Products.Where(p => pids.Contains(p.ProductId)).ToDictionary(p => p.ProductId, p => p.Slug);
            foreach (var b in best)
                vm.BestSellers.Add(new BestSellerViewModel
                {
                    ProductId = b.ProductId, ProductName = b.ProductName,
                    Slug = slugs.ContainsKey(b.ProductId) ? slugs[b.ProductId] : b.ProductId.ToString(),
                    UnitsSold = b.Units, Revenue = b.Revenue
                });

            var since = DateTime.UtcNow.Date.AddDays(-13);
            var perDay = _db.Orders.Where(o => !ExcludedStatuses.Contains(o.Status) && o.CreatedUtc >= since)
                .GroupBy(o => DbFunctions.TruncateTime(o.CreatedUtc))
                .Select(g => new { Day = g.Key, Orders = g.Count() }).ToList();
            var unitsDay = _db.OrderLines.Where(l => !ExcludedStatuses.Contains(l.Order.Status) && l.Order.CreatedUtc >= since)
                .GroupBy(l => DbFunctions.TruncateTime(l.Order.CreatedUtc))
                .Select(g => new { Day = g.Key, Units = g.Sum(l => (int?)l.Quantity) ?? 0, Revenue = g.Sum(l => (decimal?)l.LineTotal) ?? 0m })
                .ToList().ToDictionary(x => x.Day, x => x);
            foreach (var d in perDay.OrderBy(x => x.Day))
            {
                var u = unitsDay.ContainsKey(d.Day) ? unitsDay[d.Day] : null;
                vm.SalesByDay.Add(new SalesByDayViewModel
                {
                    Day = d.Day ?? since, Orders = d.Orders,
                    Units = u != null ? u.Units : 0, Revenue = u != null ? u.Revenue : 0m
                });
            }
            return vm;
        }

        public ProductDetailViewModel GetProductForAdmin(int productId)
        {
            return _catalog.GetProductDetail(productId.ToString(), true);
        }

        public OperationResult UpdateStock(int productId, int stockQuantity)
        {
            if (stockQuantity < 0 || stockQuantity > 100000) return OperationResult.Fail("Stock must be between 0 and 100000.");
            var p = _db.Products.Find(productId);
            if (p == null) return OperationResult.Fail("Product not found.");
            p.StockQuantity = stockQuantity;
            _db.SaveChanges();
            return OperationResult.Ok("Stock updated to " + stockQuantity + ".");
        }

        public OperationResult SetActive(int productId, bool isActive)
        {
            var p = _db.Products.Find(productId);
            if (p == null) return OperationResult.Fail("Product not found.");
            p.IsActive = isActive;
            _db.SaveChanges();
            return OperationResult.Ok(isActive ? "Product activated." : "Product deactivated.");
        }

        public bool IsAdmin(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            const string sql = @"SELECT CASE WHEN EXISTS(
                SELECT 1 FROM dbo.AspNetUsers u
                JOIN dbo.AspNetUserRoles ur ON ur.UserId = u.Id
                JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
                WHERE u.Email = @p0 AND r.Name = 'Admin') THEN 1 ELSE 0 END";
            return _db.Database.SqlQuery<int>(sql, email).FirstOrDefault() == 1;
        }
    }
}
