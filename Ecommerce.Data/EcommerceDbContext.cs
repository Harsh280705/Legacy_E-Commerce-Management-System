using System.Data.Entity;
using Ecommerce.Core.Entities;

namespace Ecommerce.Data
{
    /// <summary>
    /// EF6 Database-First context. The EDMX file (EcommerceModel.edmx) documents the
    /// conceptual model; this code-first mapping mirrors it 1:1 so the app runs on
    /// Mono without EDMX codegen, while the .edmx remains the design artefact.
    /// Connection string name: EcommerceDb (EntityClient with CSDL/SSDL/MSL metadata
    /// when EDMX codegen is used, or plain SqlClient — both supported via factory).
    /// </summary>
    public class EcommerceDbContext : DbContext
    {
        public EcommerceDbContext() : base("name=EcommerceDb") { }
        public EcommerceDbContext(string connectionString) : base(connectionString) { }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductImage> ProductImages { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderLine> OrderLines { get; set; }
        public virtual DbSet<Address> Addresses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().ToTable("Categories").HasKey(c => c.CategoryId);
            modelBuilder.Entity<Category>().Property(c => c.Name).IsRequired().HasMaxLength(150);
            modelBuilder.Entity<Category>().Property(c => c.Slug).IsRequired().HasMaxLength(150);
            modelBuilder.Entity<Category>().HasOptional(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories).HasForeignKey(c => c.ParentCategoryId);

            modelBuilder.Entity<Product>().ToTable("Products").HasKey(p => p.ProductId);
            modelBuilder.Entity<Product>().Property(p => p.Name).IsRequired().HasMaxLength(250);
            modelBuilder.Entity<Product>().Property(p => p.Slug).IsRequired().HasMaxLength(250);
            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().Property(p => p.CompareAtPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Product>().HasRequired(p => p.Category)
                .WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<ProductImage>().ToTable("ProductImages").HasKey(i => i.ProductImageId);
            modelBuilder.Entity<ProductImage>().HasRequired(i => i.Product)
                .WithMany(p => p.Images).HasForeignKey(i => i.ProductId).WillCascadeOnDelete(true);

            modelBuilder.Entity<ProductVariant>().ToTable("ProductVariants").HasKey(v => v.ProductVariantId);
            modelBuilder.Entity<ProductVariant>().Property(v => v.PriceAdjustment).HasPrecision(18, 2);
            modelBuilder.Entity<ProductVariant>().HasRequired(v => v.Product)
                .WithMany(p => p.Variants).HasForeignKey(v => v.ProductId).WillCascadeOnDelete(true);

            modelBuilder.Entity<Customer>().ToTable("Customers").HasKey(c => c.CustomerId);
            modelBuilder.Entity<Customer>().Property(c => c.Email).IsRequired().HasMaxLength(256);

            modelBuilder.Entity<CartItem>().ToTable("CartItems").HasKey(c => c.CartItemId);
            modelBuilder.Entity<CartItem>().Property(c => c.UnitPrice).HasPrecision(18, 2);

            modelBuilder.Entity<Order>().ToTable("Orders").HasKey(o => o.OrderId);
            modelBuilder.Entity<Order>().Property(o => o.SubTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.ShippingCost).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.TaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.GrandTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().HasOptional(o => o.Customer)
                .WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId);

            modelBuilder.Entity<OrderLine>().ToTable("OrderLines").HasKey(l => l.OrderLineId);
            modelBuilder.Entity<OrderLine>().Property(l => l.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<OrderLine>().Property(l => l.LineTotal).HasPrecision(18, 2);
            modelBuilder.Entity<OrderLine>().HasRequired(l => l.Order)
                .WithMany(o => o.Lines).HasForeignKey(l => l.OrderId).WillCascadeOnDelete(true);

            modelBuilder.Entity<Address>().ToTable("Addresses").HasKey(a => a.AddressId);
            modelBuilder.Entity<Address>().HasOptional(a => a.Customer)
                .WithMany(c => c.Addresses).HasForeignKey(a => a.CustomerId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
