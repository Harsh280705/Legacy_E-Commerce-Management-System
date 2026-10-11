using Ecommerce.Core.Entities;
using Ecommerce.Core.ViewModels;

namespace Ecommerce.Services
{
    public static class Mapping
    {
        public static ProductCardViewModel ToCard(Product p)
        {
            int? discount = null;
            if (p.CompareAtPrice.HasValue && p.CompareAtPrice.Value > p.Price && p.CompareAtPrice.Value > 0)
                discount = (int)System.Math.Round((double)((p.CompareAtPrice.Value - p.Price) / p.CompareAtPrice.Value * 100));
            else if (p.DiscountPercent.HasValue)
                discount = (int)p.DiscountPercent.Value;
            string img = "/Images/placeholder.png";
            if (p.Images != null)
                foreach (var i in p.Images) { img = i.ImageUrl; break; }
            return new ProductCardViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Slug = p.Slug,
                ImageUrl = img,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                DiscountPct = discount,
                StockQuantity = p.StockQuantity,
                InStock = p.StockQuantity > 0,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                IsFeatured = p.IsFeatured,
                IsNewArrival = p.IsNewArrival
            };
        }
    }

    /// <summary>Centralised pricing: shipping tiers + demo tax. Server-side only.</summary>
    public static class PricingRules
    {
        public static decimal ShippingFor(decimal subtotal, string method)
        {
            if (subtotal <= 0) return 0;
            string m = (method ?? "standard").ToLowerInvariant();
            if (m == "express") return subtotal >= 150 ? 0 : 12.99m;
            if (m == "priority") return subtotal >= 200 ? 0 : 24.99m;
            return subtotal >= 75 ? 0 : 5.99m; // standard
        }
        public static decimal TaxFor(decimal subtotal) { return System.Math.Round(subtotal * 0.08m, 2); }
        public static decimal EffectivePrice(Product p, ProductVariant v)
        {
            decimal base_ = p.Price;
            if (v != null && v.PriceAdjustment.HasValue) base_ += v.PriceAdjustment.Value;
            return base_;
        }
    }
}
