using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Core.ViewModels
{
    public class CategoryNavViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string ImageUrl { get; set; }
        public int ProductCount { get; set; }
        public IList<CategoryNavViewModel> Children { get; set; }
        public CategoryNavViewModel() { Children = new List<CategoryNavViewModel>(); }
    }

    public class ProductCardViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int? DiscountPct { get; set; }
        public int StockQuantity { get; set; }
        public bool InStock { get; set; }
        public string CategoryName { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsNewArrival { get; set; }
    }

    public class ProductDetailViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int? DiscountPct { get; set; }
        public int StockQuantity { get; set; }
        public bool InStock { get; set; }
        public bool IsActive { get; set; }
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }
        public IList<ProductImageVm> Images { get; set; }
        public IList<ProductVariantVm> Variants { get; set; }
        public IList<ProductCardViewModel> Related { get; set; }
        public ProductDetailViewModel() { Images = new List<ProductImageVm>(); Variants = new List<ProductVariantVm>(); Related = new List<ProductCardViewModel>(); }
    }
    public class ProductImageVm { public string ImageUrl { get; set; } public string AltText { get; set; } }
    public class ProductVariantVm { public int VariantId { get; set; } public string Name { get; set; } public string Value { get; set; } public string Sku { get; set; } public decimal? PriceAdjustment { get; set; } public int Stock { get; set; } }

    public class CategoryPageViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public IList<CategoryNavViewModel> ChildCategories { get; set; }
        public IList<ProductCardViewModel> Products { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string Sort { get; set; }
        public CategoryPageViewModel() { ChildCategories = new List<CategoryNavViewModel>(); Products = new List<ProductCardViewModel>(); }
    }

    public class CartLineViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSlug { get; set; }
        public string ImageUrl { get; set; }
        public int? VariantId { get; set; }
        public string VariantLabel { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
        public int StockAvailable { get; set; }
    }

    public class CartViewModel
    {
        public IList<CartLineViewModel> Lines { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }
        public int ItemCount { get; set; }
        public CartViewModel() { Lines = new List<CartLineViewModel>(); }
    }

    public class CheckoutFormViewModel
    {
        [Required, StringLength(100)] public string FullName { get; set; }
        [Required, EmailAddress, StringLength(256)] public string Email { get; set; }
        [Required, StringLength(200)] public string AddressLine1 { get; set; }
        [StringLength(200)] public string AddressLine2 { get; set; }
        [Required, StringLength(100)] public string City { get; set; }
        [Required, StringLength(100)] public string State { get; set; }
        [Required, StringLength(20)] public string PostalCode { get; set; }
        [Required, StringLength(100)] public string Country { get; set; }
        [StringLength(30)] public string Phone { get; set; }
        [Required] public string ShippingMethod { get; set; }
        [Required] public string PaymentMethod { get; set; }
    }

    public class CheckoutSummaryViewModel
    {
        public CartViewModel Cart { get; set; }
        public CheckoutFormViewModel Form { get; set; }
        public CheckoutSummaryViewModel() { Cart = new CartViewModel(); Form = new CheckoutFormViewModel(); }
    }

    public class OrderConfirmationViewModel
    {
        public string OrderNumber { get; set; }
        public string Email { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; }
        public DateTime CreatedUtc { get; set; }
        public IList<CartLineViewModel> Lines { get; set; }
        public OrderConfirmationViewModel() { Lines = new List<CartLineViewModel>(); }
    }

    public class OrderHistoryItemViewModel
    {
        public string OrderNumber { get; set; }
        public DateTime CreatedUtc { get; set; }
        public decimal GrandTotal { get; set; }
        public string Status { get; set; }
        public int ItemCount { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, EmailAddress] public string Email { get; set; }
        [Required, StringLength(100, MinimumLength = 6)] public string Password { get; set; }
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
    }

    public class LoginViewModel
    {
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}
