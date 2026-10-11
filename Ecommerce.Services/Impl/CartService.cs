using System;
using System.Data.Entity;
using System.Linq;
using Ecommerce.Core.Common;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data;

namespace Ecommerce.Services.Impl
{
    public class CartService : ICartService
    {
        private readonly EcommerceDbContext _db;
        public CartService(EcommerceDbContext db) { _db = db; }

        public CartViewModel GetCart(string cartSessionId)
        {
            var vm = new CartViewModel();
            if (string.IsNullOrEmpty(cartSessionId)) return vm;
            var items = _db.CartItems.Include(c => c.Product).Include(c => c.Variant)
                .Where(c => c.CartSessionId == cartSessionId).OrderBy(c => c.AddedUtc).ToList();
            foreach (var ci in items)
            {
                string img = _db.ProductImages.Where(i => i.ProductId == ci.ProductId)
                    .OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault() ?? "/Images/placeholder.png";
                string variantLabel = ci.Variant != null ? (ci.Variant.VariantName + ": " + ci.Variant.VariantValue) : null;
                vm.Lines.Add(new CartLineViewModel
                {
                    CartItemId = ci.CartItemId, ProductId = ci.ProductId,
                    ProductName = ci.Product != null ? ci.Product.Name : "Product",
                    ProductSlug = ci.Product != null ? ci.Product.Slug : ci.ProductId.ToString(),
                    ImageUrl = img, VariantId = ci.ProductVariantId, VariantLabel = variantLabel,
                    UnitPrice = ci.UnitPrice, Quantity = ci.Quantity,
                    LineTotal = ci.UnitPrice * ci.Quantity,
                    StockAvailable = ci.Variant != null ? ci.Variant.StockQuantity : (ci.Product != null ? ci.Product.StockQuantity : 0)
                });
            }
            vm.SubTotal = vm.Lines.Sum(l => l.LineTotal);
            vm.Shipping = PricingRules.ShippingFor(vm.SubTotal, "standard");
            vm.Tax = PricingRules.TaxFor(vm.SubTotal);
            vm.GrandTotal = vm.SubTotal + vm.Shipping + vm.Tax;
            vm.ItemCount = vm.Lines.Sum(l => l.Quantity);
            return vm;
        }

        public OperationResult AddToCart(string cartSessionId, int productId, int? variantId, int quantity)
        {
            if (string.IsNullOrEmpty(cartSessionId)) return OperationResult.Fail("Session is missing.");
            if (quantity < 1) quantity = 1;
            if (quantity > 99) return OperationResult.Fail("Maximum 99 units per add.");
            var product = _db.Products.Include(p => p.Variants).FirstOrDefault(p => p.ProductId == productId && p.IsActive);
            if (product == null) return OperationResult.Fail("Product not found.");
            ProductVariant variant = null;
            int available = product.StockQuantity;
            decimal unitPrice = product.Price;
            if (variantId.HasValue)
            {
                variant = product.Variants.FirstOrDefault(v => v.ProductVariantId == variantId.Value);
                if (variant == null) return OperationResult.Fail("Variant not found.");
                available = variant.StockQuantity;
                unitPrice = PricingRules.EffectivePrice(product, variant);
            }
            if (available <= 0) return OperationResult.Fail("This item is out of stock.");
            var existing = _db.CartItems.FirstOrDefault(c => c.CartSessionId == cartSessionId && c.ProductId == productId && c.ProductVariantId == variantId);
            int newQty = quantity + (existing != null ? existing.Quantity : 0);
            if (newQty > available) return OperationResult.Fail("Only " + available + " unit(s) available.");
            if (existing != null) { existing.Quantity = newQty; existing.UnitPrice = unitPrice; }
            else _db.CartItems.Add(new CartItem
            {
                CartSessionId = cartSessionId, ProductId = productId, ProductVariantId = variantId,
                Quantity = quantity, UnitPrice = unitPrice, AddedUtc = DateTime.UtcNow
            });
            _db.SaveChanges();
            return OperationResult.Ok("Added to cart.");
        }

        public OperationResult UpdateQuantity(string cartSessionId, int cartItemId, int quantity)
        {
            var item = _db.CartItems.Include(c => c.Product).Include(c => c.Variant)
                .FirstOrDefault(c => c.CartItemId == cartItemId && c.CartSessionId == cartSessionId);
            if (item == null) return OperationResult.Fail("Cart item not found.");
            if (quantity <= 0) { _db.CartItems.Remove(item); _db.SaveChanges(); return OperationResult.Ok("Removed."); }
            if (quantity > 99) return OperationResult.Fail("Maximum 99 units.");
            int available = item.Variant != null ? item.Variant.StockQuantity : (item.Product != null ? item.Product.StockQuantity : 0);
            if (quantity > available) return OperationResult.Fail("Only " + available + " unit(s) available.");
            item.Quantity = quantity;
            _db.SaveChanges();
            return OperationResult.Ok("Quantity updated.");
        }

        public OperationResult RemoveItem(string cartSessionId, int cartItemId)
        {
            var item = _db.CartItems.FirstOrDefault(c => c.CartItemId == cartItemId && c.CartSessionId == cartSessionId);
            if (item == null) return OperationResult.Fail("Cart item not found.");
            _db.CartItems.Remove(item);
            _db.SaveChanges();
            return OperationResult.Ok("Removed.");
        }

        public OperationResult ClearCart(string cartSessionId)
        {
            var items = _db.CartItems.Where(c => c.CartSessionId == cartSessionId).ToList();
            foreach (var i in items) _db.CartItems.Remove(i);
            _db.SaveChanges();
            return OperationResult.Ok();
        }

        public int GetCartCount(string cartSessionId)
        {
            if (string.IsNullOrEmpty(cartSessionId)) return 0;
            return _db.CartItems.Where(c => c.CartSessionId == cartSessionId).Select(c => (int?)c.Quantity).Sum() ?? 0;
        }
    }
}
