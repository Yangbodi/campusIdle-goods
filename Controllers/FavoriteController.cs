using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusIdleGoods.Models;
using CampusIdleGoods.Data;
using CampusIdleGoods.Helpers;
using CampusIdleGoods.Attributes;

namespace CampusIdleGoods.Controllers
{
    /// <summary>
    /// 收藏控制器
    /// </summary>
    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(
            ApplicationDbContext context,
            ILogger<FavoriteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 我的收藏列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var pageSize = 20;
            var query = _context.Favorites
                .Where(f => f.UserId == userId.Value)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Seller)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Images.OrderBy(i => i.SortOrder))
                .Where(f => f.Product.Status == ProductStatus.Published)
                .OrderByDescending(f => f.CreatedAt);

            var totalCount = await query.CountAsync();
            var favorites = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Favorites = favorites;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View();
        }

        /// <summary>
        /// 添加收藏
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "商品不存在" });
            }

            // 检查是否已收藏
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ProductId == productId);

            if (existingFavorite != null)
            {
                return Json(new { success = false, message = "已收藏过该商品" });
            }

            try
            {
                var favorite = new Favorite
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    CreatedAt = DateTime.Now
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"用户 {userId.Value} 收藏商品 {productId}");

                return Json(new { success = true, message = "收藏成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加收藏时发生错误");
                return Json(new { success = false, message = "收藏失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 取消收藏
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ProductId == productId);

            if (favorite == null)
            {
                return Json(new { success = false, message = "未收藏该商品" });
            }

            try
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"用户 {userId.Value} 取消收藏商品 {productId}");

                return Json(new { success = true, message = "已取消收藏" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消收藏时发生错误");
                return Json(new { success = false, message = "操作失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 切换收藏状态（AJAX）
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录", isFavorite = false });
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ProductId == productId);

            try
            {
                if (favorite == null)
                {
                    // 添加收藏
                    var product = await _context.Products.FindAsync(productId);
                    if (product == null)
                    {
                        return Json(new { success = false, message = "商品不存在", isFavorite = false });
                    }

                    favorite = new Favorite
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        CreatedAt = DateTime.Now
                    };

                    _context.Favorites.Add(favorite);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "收藏成功", isFavorite = true });
                }
                else
                {
                    // 取消收藏
                    _context.Favorites.Remove(favorite);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "已取消收藏", isFavorite = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换收藏状态时发生错误");
                return Json(new { success = false, message = "操作失败", isFavorite = favorite != null });
            }
        }

        /// <summary>
        /// 检查是否已收藏（AJAX）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Check(int productId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { isFavorite = false });
            }

            var isFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userId.Value && f.ProductId == productId);

            return Json(new { isFavorite });
        }
    }
}

