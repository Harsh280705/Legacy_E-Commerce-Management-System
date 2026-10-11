using System;
using System.Collections.Generic;

namespace Ecommerce.Core.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int? ParentCategoryId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public virtual Category ParentCategory { get; set; }
        public virtual ICollection<Category> ChildCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public Category() { ChildCategories = new List<Category>(); Products = new List<Product>(); }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedUtc { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<ProductImage> Images { get; set; }
        public virtual ICollection<ProductVariant> Variants { get; set; }
        public Product() { Images = new List<ProductImage>(); Variants = new List<ProductVariant>(); }
    }

    public class ProductImage
    {
        public int ProductImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public string AltText { get; set; }
        public int DisplayOrder { get; set; }
        public virtual Product Product { get; set; }
    }

    public class ProductVariant
    {
        public int ProductVariantId { get; set; }
        public int ProductId { get; set; }
        public string VariantName { get; set; } // e.g. Size, Colour
        public string VariantValue { get; set; } // e.g. XL, Red
        public string Sku { get; set; }
        public decimal? PriceAdjustment { get; set; }
        public int StockQuantity { get; set; }
        public virtual Product Product { get; set; }
    }

    public class Customer
    {
        public int CustomerId { get; set; }
        public string AspNetUserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedUtc { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Address> Addresses { get; set; }
        public Customer() { Orders = new List<Order>(); Addresses = new List<Address>(); }
    }

    public class CartItem
    {
        public int CartItemId { get; set; }
        public string CartSessionId { get; set; }
        public int? CustomerId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime AddedUtc { get; set; }
        public virtual Product Product { get; set; }
        public virtual ProductVariant Variant { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerEmail { get; set; }
        public string ShippingName { get; set; }
        public string ShippingAddressLine1 { get; set; }
        public string ShippingAddressLine2 { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingPostalCode { get; set; }
        public string ShippingCountry { get; set; }
        public string ShippingMethod { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public string IdempotencyKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual ICollection<OrderLine> Lines { get; set; }
        public Order() { Lines = new List<OrderLine>(); }
    }

    public class OrderLine
    {
        public int OrderLineId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public string ProductName { get; set; }
        public string VariantLabel { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }

    public class Address
    {
        public int AddressId { get; set; }
        public int? CustomerId { get; set; }
        public string Label { get; set; }
        public string FullName { get; set; }
        public string Line1 { get; set; }
        public string Line2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public bool IsDefault { get; set; }
        public virtual Customer Customer { get; set; }
    }
}
