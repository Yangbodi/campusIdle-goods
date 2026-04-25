using System.Diagnostics;
using CampusIdleGoods.Models;
using CampusIdleGoods.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusIdleGoods.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 获取最新上架的商品（最多12个）
            var latestProducts = await _context.Products
                .Where(p => p.Status == ProductStatus.Published)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            // 获取热门分类（有商品数量最多的分类）
            var popularCategories = await _context.Categories
                .Where(c => c.IsEnabled && c.ParentId == null)
                .Select(c => new
                {
                    Category = c,
                    ProductCount = _context.Products.Count(p => p.CategoryId == c.Id && p.Status == ProductStatus.Published)
                })
                .OrderByDescending(x => x.ProductCount)
                .Take(4)
                .Select(x => x.Category)
                .ToListAsync();

            ViewBag.LatestProducts = latestProducts;
            ViewBag.PopularCategories = popularCategories;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
