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
    public class CatalogService : ICatalogService
    {
        private readonly EcommerceDbContext _db;
        public CatalogService(EcommerceDbContext db) { _db = db; }

        public PagedResult<ProductCardViewModel> SearchProducts(string query, int? categoryId, string sort, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 60) pageSize = 12;
            var q = _db.Products.Include(p => p.Category).Include(p => p.Images).Where(p => p.IsActive);
            if (categoryId.HasValue)
            {
                var ids = new List<int> { categoryId.Value };
                ids.AddRange(_db.Categories.Where(c => c.ParentCategoryId == categoryId.Value).Select(c => c.CategoryId).ToList());
                q = q.Where(p => ids.Contains(p.CategoryId));
            }
            if (!string.IsNullOrWhiteSpace(query))
            {
                string s = query.Trim().ToLower();
                q = q.Where(p => p.Name.ToLower().Contains(s) || p.Description.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
            }
            q = ApplySort(q, sort);
            int total = q.Count();
            int totalPages = pageSize <= 0 ? 0 : (total + pageSize - 1) / pageSize;
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList().Select(Mapping.ToCard).ToList();
            return new PagedResult<ProductCardViewModel> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        private static IQueryable<Product> ApplySort(IQueryable<Product> q, string sort)
        {
            // Deterministic secondary key (ProductId) keeps pagination stable.
            // Default ("recommended"): curated items first, then newest. Seeded
            // same-name variants carry interleaved timestamps, so this also keeps
            // first-page grids photo-diverse instead of clustering one family.
            switch ((sort ?? "").ToLowerInvariant())
            {
                case "price_asc": return q.OrderBy(p => p.Price).ThenBy(p => p.ProductId);
                case "price_desc": return q.OrderByDescending(p => p.Price).ThenBy(p => p.ProductId);
                case "name_desc": return q.OrderByDescending(p => p.Name).ThenBy(p => p.ProductId);
                case "name_asc": return q.OrderBy(p => p.Name).ThenBy(p => p.ProductId);
                case "newest": return q.OrderByDescending(p => p.CreatedUtc).ThenBy(p => p.ProductId);
                default: return q.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.IsNewArrival).ThenByDescending(p => p.CreatedUtc).ThenBy(p => p.ProductId);
            }
        }

        public ProductDetailViewModel GetProductDetail(string slugOrId, bool includeInactive = false)
        {
            Product p = null;
            int id;
            if (int.TryParse(slugOrId, out id))
                p = _db.Products.Include(x => x.Category).Include(x => x.Images).Include(x => x.Variants).FirstOrDefault(x => x.ProductId == id && (includeInactive || x.IsActive));
            else
                p = _db.Products.Include(x => x.Category).Include(x => x.Images).Include(x => x.Variants).FirstOrDefault(x => x.Slug == slugOrId && (includeInactive || x.IsActive));
            if (p == null) return null;
            var vm = new ProductDetailViewModel
            {
                ProductId = p.ProductId, Name = p.Name, Slug = p.Slug,
                Description = p.Description, ShortDescription = p.ShortDescription,
                Price = p.Price, CompareAtPrice = p.CompareAtPrice,
                StockQuantity = p.StockQuantity, InStock = p.StockQuantity > 0,
                IsActive = p.IsActive,
                CategoryName = p.Category != null ? p.Category.Name : "",
                CategorySlug = p.Category != null ? p.Category.Slug : ""
            };
            if (p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price && p.CompareAtPrice > 0)
                vm.DiscountPct = (int)Math.Round((double)((p.CompareAtPrice.Value - p.Price) / p.CompareAtPrice.Value * 100));
            foreach (var i in p.Images.OrderBy(x => x.DisplayOrder))
                vm.Images.Add(new ProductImageVm { ImageUrl = i.ImageUrl, AltText = i.AltText });
            if (vm.Images.Count == 0) vm.Images.Add(new ProductImageVm { ImageUrl = "/Images/placeholder.png", AltText = p.Name });
            foreach (var v in p.Variants)
                vm.Variants.Add(new ProductVariantVm { VariantId = v.ProductVariantId, Name = v.VariantName, Value = v.VariantValue, Sku = v.Sku, PriceAdjustment = v.PriceAdjustment, Stock = v.StockQuantity });
            vm.Related = _db.Products.Include(x => x.Images).Include(x => x.Category)
                .Where(x => x.IsActive && x.CategoryId == p.CategoryId && x.ProductId != p.ProductId)
                .OrderByDescending(x => x.CreatedUtc).Take(4).ToList().Select(Mapping.ToCard).ToList();
            return vm;
        }

        public IList<CategoryNavViewModel> GetCategoryTree()
        {
            var cats = _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
            var counts = _db.Products.Where(p => p.IsActive).GroupBy(p => p.CategoryId).ToDictionary(g => g.Key, g => g.Count());
            var lookup = cats.ToDictionary(c => c.CategoryId, c => new CategoryNavViewModel
            {
                CategoryId = c.CategoryId, Name = c.Name, Slug = c.Slug, ImageUrl = c.ImageUrl,
                ProductCount = counts.ContainsKey(c.CategoryId) ? counts[c.CategoryId] : 0
            });
            var roots = new List<CategoryNavViewModel>();
            foreach (var c in cats)
            {
                var vm = lookup[c.CategoryId];
                if (c.ParentCategoryId.HasValue && lookup.ContainsKey(c.ParentCategoryId.Value))
                    lookup[c.ParentCategoryId.Value].Children.Add(vm);
                else
                    roots.Add(vm);
            }
            // roll child counts into parents for nicer nav badges
            foreach (var r in roots) r.ProductCount += r.Children.Sum(x => x.ProductCount);
            return roots;
        }

        public IList<ProductCardViewModel> GetFeatured(int count)
        {
            return _db.Products.Include(p => p.Category).Include(p => p.Images)
                .Where(p => p.IsActive && p.IsFeatured).OrderByDescending(p => p.CreatedUtc)
                .Take(count).ToList().Select(Mapping.ToCard).ToList();
        }
        public IList<ProductCardViewModel> GetNewArrivals(int count)
        {
            return _db.Products.Include(p => p.Category).Include(p => p.Images)
                .Where(p => p.IsActive && p.IsNewArrival).OrderByDescending(p => p.CreatedUtc)
                .Take(count).ToList().Select(Mapping.ToCard).ToList();
        }
        public IList<ProductCardViewModel> GetDiscounted(int count)
        {
            return _db.Products.Include(p => p.Category).Include(p => p.Images)
                .Where(p => p.IsActive && p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price)
                .OrderByDescending(p => p.CompareAtPrice - p.Price)
                .Take(count).ToList().Select(Mapping.ToCard).ToList();
        }

        public CategoryPageViewModel GetCategoryPage(string slug, string sort, int page, int pageSize)
        {
            var cat = _db.Categories.FirstOrDefault(c => c.Slug == slug && c.IsActive);
            if (cat == null) return null;
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 60) pageSize = 12;
            var childIds = _db.Categories.Where(c => c.ParentCategoryId == cat.CategoryId && c.IsActive).Select(c => c.CategoryId).ToList();
            var allIds = new List<int> { cat.CategoryId };
            allIds.AddRange(childIds);
            var q = _db.Products.Include(p => p.Category).Include(p => p.Images)
                .Where(p => p.IsActive && allIds.Contains(p.CategoryId));
            q = ApplySort(q, sort);
            int total = q.Count();
            int totalPages = pageSize <= 0 ? 0 : (total + pageSize - 1) / pageSize;
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var vm = new CategoryPageViewModel
            {
                CategoryId = cat.CategoryId, Name = cat.Name, Slug = cat.Slug, Description = cat.Description,
                TotalCount = total, Page = page, PageSize = pageSize, Sort = sort,
                TotalPages = totalPages
            };
            vm.Products = q.Skip((page - 1) * pageSize).Take(pageSize).ToList().Select(Mapping.ToCard).ToList();
            var children = _db.Categories.Where(c => c.ParentCategoryId == cat.CategoryId && c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
            foreach (var ch in children)
                vm.ChildCategories.Add(new CategoryNavViewModel { CategoryId = ch.CategoryId, Name = ch.Name, Slug = ch.Slug, ImageUrl = ch.ImageUrl });
            return vm;
        }
    }
}
