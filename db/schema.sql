-- LegacyEcommerceDb schema (SQL Server, idempotent: CREATE TABLE IF NOT EXISTS style).
-- Safe to run repeatedly: never drops tables, only creates missing objects.
IF DB_ID('LegacyEcommerceDb') IS NULL
BEGIN
    CREATE DATABASE LegacyEcommerceDb;
END
GO
USE LegacyEcommerceDb;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID('dbo.Categories','U') IS NULL
CREATE TABLE dbo.Categories(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Slug NVARCHAR(150) NOT NULL UNIQUE,
    Description NVARCHAR(1000) NULL,
    ImageUrl NVARCHAR(500) NULL,
    ParentCategoryId INT NULL REFERENCES dbo.Categories(CategoryId),
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);
GO
IF OBJECT_ID('dbo.Products','U') IS NULL
CREATE TABLE dbo.Products(
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    Sku NVARCHAR(64) NULL,
    Name NVARCHAR(250) NOT NULL,
    Slug NVARCHAR(250) NOT NULL UNIQUE,
    ShortDescription NVARCHAR(500) NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    CompareAtPrice DECIMAL(18,2) NULL,
    DiscountPercent DECIMAL(5,2) NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    CategoryId INT NOT NULL REFERENCES dbo.Categories(CategoryId),
    IsFeatured BIT NOT NULL DEFAULT 0,
    IsNewArrival BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
IF OBJECT_ID('dbo.ProductImages','U') IS NULL
CREATE TABLE dbo.ProductImages(
    ProductImageId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL REFERENCES dbo.Products(ProductId) ON DELETE CASCADE,
    ImageUrl NVARCHAR(500) NULL,
    AltText NVARCHAR(250) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0
);
GO
IF OBJECT_ID('dbo.ProductVariants','U') IS NULL
CREATE TABLE dbo.ProductVariants(
    ProductVariantId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL REFERENCES dbo.Products(ProductId) ON DELETE CASCADE,
    VariantName NVARCHAR(100) NULL,
    VariantValue NVARCHAR(100) NULL,
    Sku NVARCHAR(64) NULL,
    PriceAdjustment DECIMAL(18,2) NULL,
    StockQuantity INT NOT NULL DEFAULT 0
);
GO
IF OBJECT_ID('dbo.Customers','U') IS NULL
CREATE TABLE dbo.Customers(
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    AspNetUserId NVARCHAR(128) NULL,
    Email NVARCHAR(256) NOT NULL,
    FirstName NVARCHAR(100) NULL,
    LastName NVARCHAR(100) NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Customers_Email')
CREATE UNIQUE INDEX IX_Customers_Email ON dbo.Customers(Email);
GO
IF OBJECT_ID('dbo.CartItems','U') IS NULL
CREATE TABLE dbo.CartItems(
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    CartSessionId NVARCHAR(128) NULL,
    CustomerId INT NULL REFERENCES dbo.Customers(CustomerId),
    ProductId INT NOT NULL REFERENCES dbo.Products(ProductId),
    ProductVariantId INT NULL REFERENCES dbo.ProductVariants(ProductVariantId),
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL,
    AddedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
IF OBJECT_ID('dbo.Orders','U') IS NULL
CREATE TABLE dbo.Orders(
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(32) NOT NULL UNIQUE,
    CustomerId INT NULL REFERENCES dbo.Customers(CustomerId),
    CustomerEmail NVARCHAR(256) NULL,
    ShippingName NVARCHAR(200) NULL,
    ShippingAddressLine1 NVARCHAR(200) NULL,
    ShippingAddressLine2 NVARCHAR(200) NULL,
    ShippingCity NVARCHAR(100) NULL,
    ShippingState NVARCHAR(100) NULL,
    ShippingPostalCode NVARCHAR(20) NULL,
    ShippingCountry NVARCHAR(100) NULL,
    ShippingMethod NVARCHAR(50) NULL,
    SubTotal DECIMAL(18,2) NOT NULL,
    ShippingCost DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    GrandTotal DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NULL,
    Status NVARCHAR(50) NULL,
    IdempotencyKey NVARCHAR(128) NULL UNIQUE,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
IF OBJECT_ID('dbo.OrderLines','U') IS NULL
CREATE TABLE dbo.OrderLines(
    OrderLineId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
    ProductId INT NOT NULL REFERENCES dbo.Products(ProductId),
    ProductVariantId INT NULL REFERENCES dbo.ProductVariants(ProductVariantId),
    ProductName NVARCHAR(250) NULL,
    VariantLabel NVARCHAR(250) NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    LineTotal DECIMAL(18,2) NOT NULL
);
GO
IF OBJECT_ID('dbo.Addresses','U') IS NULL
CREATE TABLE dbo.Addresses(
    AddressId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NULL REFERENCES dbo.Customers(CustomerId),
    Label NVARCHAR(100) NULL,
    FullName NVARCHAR(200) NULL,
    Line1 NVARCHAR(200) NULL,
    Line2 NVARCHAR(200) NULL,
    City NVARCHAR(100) NULL,
    State NVARCHAR(100) NULL,
    PostalCode NVARCHAR(20) NULL,
    Country NVARCHAR(100) NULL,
    Phone NVARCHAR(30) NULL,
    IsDefault BIT NOT NULL DEFAULT 0
);
GO

-- ASP.NET Identity 2 tables (minimal set used by Identity.EntityFramework 2.2.3).
IF OBJECT_ID('dbo.AspNetRoles','U') IS NULL
CREATE TABLE dbo.AspNetRoles(Id NVARCHAR(128) PRIMARY KEY, Name NVARCHAR(256) NOT NULL);
GO
IF OBJECT_ID('dbo.AspNetUsers','U') IS NULL
CREATE TABLE dbo.AspNetUsers(
    Id NVARCHAR(128) PRIMARY KEY,
    Email NVARCHAR(256) NULL, EmailConfirmed BIT NOT NULL DEFAULT 0,
    PasswordHash NVARCHAR(MAX) NULL, SecurityStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL, PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0, LockoutEndDateUtc DATETIME NULL,
    LockoutEnabled BIT NOT NULL DEFAULT 1, AccessFailedCount INT NOT NULL DEFAULT 0,
    UserName NVARCHAR(256) NOT NULL, FirstName NVARCHAR(100) NULL, LastName NVARCHAR(100) NULL
);
GO
IF OBJECT_ID('dbo.AspNetUserRoles','U') IS NULL
CREATE TABLE dbo.AspNetUserRoles(UserId NVARCHAR(128) NOT NULL REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE, RoleId NVARCHAR(128) NOT NULL REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE, PRIMARY KEY(UserId, RoleId));
GO
IF OBJECT_ID('dbo.AspNetUserClaims','U') IS NULL
CREATE TABLE dbo.AspNetUserClaims(Id INT IDENTITY(1,1) PRIMARY KEY, UserId NVARCHAR(128) NOT NULL REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE, ClaimType NVARCHAR(MAX) NULL, ClaimValue NVARCHAR(MAX) NULL);
GO
IF OBJECT_ID('dbo.AspNetUserLogins','U') IS NULL
CREATE TABLE dbo.AspNetUserLogins(LoginProvider NVARCHAR(128) NOT NULL, ProviderKey NVARCHAR(128) NOT NULL, UserId NVARCHAR(128) NOT NULL REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE, PRIMARY KEY(LoginProvider, ProviderKey, UserId));
GO

-- Helpful indexes (created only if missing).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Products_Category') CREATE INDEX IX_Products_Category ON dbo.Products(CategoryId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Products_Slug') CREATE UNIQUE INDEX IX_Products_Slug ON dbo.Products(Slug);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_CartItems_Session') CREATE INDEX IX_CartItems_Session ON dbo.CartItems(CartSessionId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Orders_Email') CREATE INDEX IX_Orders_Email ON dbo.Orders(CustomerEmail);
GO
PRINT 'Schema OK';
