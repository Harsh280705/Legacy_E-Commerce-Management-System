using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Ecommerce.Core.Common;
using Ecommerce.Core.ViewModels;

namespace Ecommerce.Core.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        T GetById(object id);
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        int Count(Expression<Func<T, bool>> predicate);
    }

    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> Repository<T>() where T : class;
        int SaveChanges();
    }

    public interface ICatalogService
    {
        PagedResult<ProductCardViewModel> SearchProducts(string query, int? categoryId, string sort, int page, int pageSize);
        ProductDetailViewModel GetProductDetail(string slugOrId, bool includeInactive = false);
        IList<CategoryNavViewModel> GetCategoryTree();
        IList<ProductCardViewModel> GetFeatured(int count);
        IList<ProductCardViewModel> GetNewArrivals(int count);
        IList<ProductCardViewModel> GetDiscounted(int count);
        CategoryPageViewModel GetCategoryPage(string slug, string sort, int page, int pageSize);
    }

    public interface ICartService
    {
        CartViewModel GetCart(string cartSessionId);
        OperationResult AddToCart(string cartSessionId, int productId, int? variantId, int quantity);
        OperationResult UpdateQuantity(string cartSessionId, int cartItemId, int quantity);
        OperationResult RemoveItem(string cartSessionId, int cartItemId);
        OperationResult ClearCart(string cartSessionId);
        int GetCartCount(string cartSessionId);
    }

    public interface ICheckoutService
    {
        CheckoutSummaryViewModel BuildSummary(string cartSessionId);
        OperationResult<OrderConfirmationViewModel> PlaceOrder(string cartSessionId, CheckoutFormViewModel form, string userEmail, string idempotencyKey);
        OrderConfirmationViewModel GetOrder(string orderNumber);
        IList<OrderHistoryItemViewModel> GetOrderHistory(string email);
    }

    public interface IAccountService
    {
        OperationResult Register(string email, string password, string firstName, string lastName);
        bool ValidateCredentials(string email, string password);
        void EnsureCustomerRecord(string aspNetUserId, string email, string firstName, string lastName);
    }

    /// <summary>Thin database-connectivity probe so controllers never touch EF.</summary>
    public interface IDatabaseHealthCheck
    {
        bool IsHealthy();
    }

    public interface IAdminService
    {
        AdminDashboardViewModel GetDashboard(int lowStockThreshold);
        ProductDetailViewModel GetProductForAdmin(int productId);
        OperationResult UpdateStock(int productId, int stockQuantity);
        OperationResult SetActive(int productId, bool isActive);
        bool IsAdmin(string email);
    }
}
