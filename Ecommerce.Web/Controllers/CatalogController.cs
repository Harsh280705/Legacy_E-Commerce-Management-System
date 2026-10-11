using System.IO;
using System.Web.Mvc;
using Ecommerce.Core.Interfaces;

namespace Ecommerce.Web.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ICatalogService _catalog;
        public CatalogController(ICatalogService catalog) { _catalog = catalog; }

        private static int PageSize(int? size)
        {
            if (!size.HasValue) return 12;
            if (size.Value == 12 || size.Value == 24) return size.Value;
            return 12;
        }

        // /Catalog/Index?q=&categoryId=&sort=&page=&size=&mode=
        public ActionResult Index(string q, int? categoryId, string sort, int page = 1, int? size = null, string mode = null)
        {
            ViewBag.Categories = _catalog.GetCategoryTree();
            ViewBag.Query = q;
            ViewBag.CategoryId = categoryId;
            ViewBag.Sort = sort ?? "recommended";
            ViewBag.Size = PageSize(size);
            ViewBag.Mode = (mode ?? "pagination").ToLowerInvariant() == "infinite" ? "infinite" : "pagination";
            var result = _catalog.SearchProducts(q, categoryId, sort, page, PageSize(size));
            return View(result);
        }

        // Pretty route /c/{slug}
        public ActionResult Category(string slug, string sort, int page = 1, int? size = null, string mode = null)
        {
            ViewBag.Categories = _catalog.GetCategoryTree();
            ViewBag.Size = PageSize(size);
            ViewBag.Mode = (mode ?? "pagination").ToLowerInvariant() == "infinite" ? "infinite" : "pagination";
            var vm = _catalog.GetCategoryPage(slug, sort, page, PageSize(size));
            if (vm == null) return HttpNotFound("Category not found.");
            vm.Sort = sort ?? "recommended";
            return View(vm);
        }

        // Pretty route /p/{slug}
        public ActionResult Product(string slug)
        {
            ViewBag.Categories = _catalog.GetCategoryTree();
            var vm = _catalog.GetProductDetail(slug);
            if (vm == null) return HttpNotFound("Product not found.");
            return View(vm);
        }

        // AJAX: live category children for lazy nav.
        [HttpGet]
        public ActionResult Children(int id)
        {
            var tree = _catalog.GetCategoryTree();
            foreach (var c in tree)
            {
                if (c.CategoryId == id) return PartialView("_CategoryChildren", c.Children);
                foreach (var ch in c.Children)
                    if (ch.CategoryId == id) return PartialView("_CategoryChildren", ch.Children);
            }
            return PartialView("_CategoryChildren", new System.Collections.Generic.List<Ecommerce.Core.ViewModels.CategoryNavViewModel>());
        }

        // AJAX: filtered product grid partial (search/sort/pagination without full reload).
        [HttpGet]
        public ActionResult Grid(string q, int? categoryId, string slug, string sort, int page = 1, int? size = null)
        {
            int ps = PageSize(size);
            if (!string.IsNullOrEmpty(slug))
            {
                var cat = _catalog.GetCategoryPage(slug, sort, page, ps);
                if (cat == null) return HttpNotFound();
                return PartialView("_ProductGrid", cat.Products);
            }
            var result = _catalog.SearchProducts(q, categoryId, sort, page, ps);
            return PartialView("_ProductGrid", result.Items);
        }

        // AJAX/JSON page for infinite scroll: server-rendered cards + paging metadata.
        // Never returns more than one page (server-side page-size limits apply).
        [HttpGet]
        public ActionResult GridData(string q, int? categoryId, string slug, string sort, int page = 1, int? size = null)
        {
            int ps = PageSize(size);
            if (page < 1) page = 1;
            System.Collections.Generic.IList<Ecommerce.Core.ViewModels.ProductCardViewModel> items;
            int total;
            int actualPage = page;
            if (!string.IsNullOrEmpty(slug))
            {
                var cat = _catalog.GetCategoryPage(slug, sort, page, ps);
                if (cat == null) return HttpNotFound();
                items = cat.Products;
                total = cat.TotalCount;
                actualPage = cat.Page;
            }
            else
            {
                var result = _catalog.SearchProducts(q, categoryId, sort, page, ps);
                items = new System.Collections.Generic.List<Ecommerce.Core.ViewModels.ProductCardViewModel>(result.Items);
                total = result.TotalCount;
                actualPage = result.Page;
            }
            string html = RenderPartialToString("_ProductGrid", items);
            bool hasMore = actualPage * ps < total;
            return Json(new { html = html, hasMore = hasMore, nextPage = actualPage + 1, total = total }, JsonRequestBehavior.AllowGet);
        }

        private string RenderPartialToString(string partialName, object model)
        {
            ViewData.Model = model;
            using (var writer = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, partialName);
                if (viewResult.View == null) return string.Empty;
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, writer);
                viewResult.View.Render(viewContext, writer);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return writer.ToString();
            }
        }
    }
}
