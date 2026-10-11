using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Core.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int TotalCategories { get; set; }
        public int LowStockThreshold { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int TotalUnitsSold { get; set; }
        public decimal TotalRecordedSales { get; set; }
        public IList<OrderHistoryItemViewModel> RecentOrders { get; set; }
        public IList<BestSellerViewModel> BestSellers { get; set; }
        public IList<SalesByDayViewModel> SalesByDay { get; set; }
        public AdminDashboardViewModel()
        {
            RecentOrders = new List<OrderHistoryItemViewModel>();
            BestSellers = new List<BestSellerViewModel>();
            SalesByDay = new List<SalesByDayViewModel>();
        }
    }

    public class BestSellerViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Slug { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class SalesByDayViewModel
    {
        public DateTime Day { get; set; }
        public int Orders { get; set; }
        public int Units { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminStockUpdateViewModel
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        [Range(0, 100000)]
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }
}
