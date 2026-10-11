using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Ecommerce.Core.Common;
using Ecommerce.Core.Entities;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data;

namespace Ecommerce.Services.Impl
{
    public class CheckoutService : ICheckoutService
    {
        private readonly EcommerceDbContext _db;
        private readonly ICartService _carts;
        public CheckoutService(EcommerceDbContext db, ICartService carts) { _db = db; _carts = carts; }

        public CheckoutSummaryViewModel BuildSummary(string cartSessionId)
        {
            var vm = new CheckoutSummaryViewModel();
            vm.Cart = _carts.GetCart(cartSessionId);
            vm.Form.Country = "India";
            vm.Form.ShippingMethod = "standard";
            vm.Form.PaymentMethod = "cod";
            return vm;
        }

        public OperationResult<OrderConfirmationViewModel> PlaceOrder(string cartSessionId, CheckoutFormViewModel form, string userEmail, string idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey)) idempotencyKey = Guid.NewGuid().ToString("N");
            // Duplicate-submit guard: same key returns the original order.
            var dup = _db.Orders.Include(o => o.Lines).FirstOrDefault(o => o.IdempotencyKey == idempotencyKey);
            if (dup != null) return OperationResult<OrderConfirmationViewModel>.Ok(ToConfirmation(dup), "Duplicate submission — returning original order.");

            var cart = _carts.GetCart(cartSessionId);
            if (cart.Lines.Count == 0) return OperationResult<OrderConfirmationViewModel>.Fail("Your cart is empty.");

            // Re-validate stock + recompute prices server-side (never trust client totals).
            decimal subtotal = 0;
            var lines = new List<OrderLine>();
            foreach (var l in cart.Lines)
            {
                var product = _db.Products.Include(p => p.Variants).FirstOrDefault(p => p.ProductId == l.ProductId);
                if (product == null || !product.IsActive) return OperationResult<OrderConfirmationViewModel>.Fail("Product '" + l.ProductName + "' is no longer available.");
                ProductVariant variant = null;
                if (l.VariantId.HasValue)
                {
                    variant = product.Variants.FirstOrDefault(v => v.ProductVariantId == l.VariantId.Value);
                    if (variant == null) return OperationResult<OrderConfirmationViewModel>.Fail("A selected variant is no longer available.");
                }
                int available = variant != null ? variant.StockQuantity : product.StockQuantity;
                if (l.Quantity > available) return OperationResult<OrderConfirmationViewModel>.Fail("Only " + available + " unit(s) of '" + product.Name + "' available.");
                decimal unit = PricingRules.EffectivePrice(product, variant);
                subtotal += unit * l.Quantity;
                lines.Add(new OrderLine
                {
                    ProductId = product.ProductId, ProductVariantId = l.VariantId,
                    ProductName = product.Name,
                    VariantLabel = variant != null ? variant.VariantName + ": " + variant.VariantValue : null,
                    Quantity = l.Quantity, UnitPrice = unit, LineTotal = unit * l.Quantity, Product = product
                });
            }
            decimal shipping = PricingRules.ShippingFor(subtotal, form.ShippingMethod);
            decimal tax = PricingRules.TaxFor(subtotal);
            decimal grand = subtotal + shipping + tax;

            var allowedShip = new[] { "standard", "express", "priority" };
            var allowedPay = new[] { "cod", "card_demo", "upi_demo", "netbanking_demo" };
            string ship = allowedShip.Contains((form.ShippingMethod ?? "").ToLower()) ? form.ShippingMethod.ToLower() : "standard";
            string pay = allowedPay.Contains((form.PaymentMethod ?? "").ToLower()) ? form.PaymentMethod.ToLower() : "cod";

            Customer customer = null;
            if (!string.IsNullOrEmpty(userEmail))
                customer = _db.Customers.FirstOrDefault(c => c.Email == userEmail);

            var order = new Order
            {
                OrderNumber = "LE-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + new Random().Next(100000, 999999),
                CustomerId = customer != null ? (int?)customer.CustomerId : null,
                CustomerEmail = string.IsNullOrEmpty(userEmail) ? form.Email : userEmail,
                ShippingName = form.FullName,
                ShippingAddressLine1 = form.AddressLine1,
                ShippingAddressLine2 = form.AddressLine2,
                ShippingCity = form.City, ShippingState = form.State,
                ShippingPostalCode = form.PostalCode, ShippingCountry = form.Country,
                ShippingMethod = ship, SubTotal = subtotal, ShippingCost = shipping,
                TaxAmount = tax, GrandTotal = grand, PaymentMethod = pay,
                Status = "Placed", IdempotencyKey = idempotencyKey, CreatedUtc = DateTime.UtcNow
            };
            foreach (var ln in lines) order.Lines.Add(ln);
            _db.Orders.Add(order);

            // Decrement stock.
            foreach (var ln in lines)
            {
                var p = _db.Products.Include(x => x.Variants).First(x => x.ProductId == ln.ProductId);
                if (ln.ProductVariantId.HasValue)
                {
                    var v = p.Variants.First(x => x.ProductVariantId == ln.ProductVariantId.Value);
                    v.StockQuantity = Math.Max(0, v.StockQuantity - ln.Quantity);
                    p.StockQuantity = Math.Max(0, p.StockQuantity - ln.Quantity);
                }
                else p.StockQuantity = Math.Max(0, p.StockQuantity - ln.Quantity);
            }
            _db.SaveChanges();
            _carts.ClearCart(cartSessionId);
            return OperationResult<OrderConfirmationViewModel>.Ok(ToConfirmation(order), "Order placed.");
        }

        private static OrderConfirmationViewModel ToConfirmation(Order o)
        {
            var vm = new OrderConfirmationViewModel
            {
                OrderNumber = o.OrderNumber, Email = o.CustomerEmail,
                GrandTotal = o.GrandTotal, Status = o.Status, CreatedUtc = o.CreatedUtc
            };
            foreach (var l in o.Lines)
                vm.Lines.Add(new CartLineViewModel { ProductName = l.ProductName, VariantLabel = l.VariantLabel, Quantity = l.Quantity, UnitPrice = l.UnitPrice, LineTotal = l.LineTotal });
            return vm;
        }

        public OrderConfirmationViewModel GetOrder(string orderNumber)
        {
            var o = _db.Orders.Include(x => x.Lines).FirstOrDefault(x => x.OrderNumber == orderNumber);
            return o == null ? null : ToConfirmation(o);
        }

        public IList<OrderHistoryItemViewModel> GetOrderHistory(string email)
        {
            if (string.IsNullOrEmpty(email)) return new List<OrderHistoryItemViewModel>();
            return _db.Orders.Where(o => o.CustomerEmail == email).OrderByDescending(o => o.CreatedUtc)
                .ToList().Select(o => new OrderHistoryItemViewModel
                {
                    OrderNumber = o.OrderNumber, CreatedUtc = o.CreatedUtc,
                    GrandTotal = o.GrandTotal, Status = o.Status, ItemCount = o.Lines.Sum(l => (int?)l.Quantity) ?? 0
                }).ToList();
        }
    }

    public class AccountService : IAccountService
    {
        private readonly EcommerceDbContext _db;
        public AccountService(EcommerceDbContext db) { _db = db; }
        public OperationResult Register(string email, string password, string firstName, string lastName)
        {
            // Identity creates the actual credential; here we just ensure the Customer row exists.
            EnsureCustomerRecord(null, email, firstName, lastName);
            return OperationResult.Ok("Registered.");
        }
        public bool ValidateCredentials(string email, string password) { return false; /* Identity handles real auth */ }
        public void EnsureCustomerRecord(string aspNetUserId, string email, string firstName, string lastName)
        {
            var c = _db.Customers.FirstOrDefault(x => x.Email == email);
            if (c == null)
            {
                _db.Customers.Add(new Customer { AspNetUserId = aspNetUserId, Email = email, FirstName = firstName, LastName = lastName, CreatedUtc = DateTime.UtcNow });
                _db.SaveChanges();
            }
            else if (!string.IsNullOrEmpty(aspNetUserId) && c.AspNetUserId != aspNetUserId)
            {
                c.AspNetUserId = aspNetUserId;
                _db.SaveChanges();
            }
        }
    }
}
