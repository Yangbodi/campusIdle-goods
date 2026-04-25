using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusIdleGoods.Models;
using CampusIdleGoods.Data;
using CampusIdleGoods.Helpers;
using CampusIdleGoods.Attributes;
using CampusIdleGoods.Models.ViewModels;

namespace CampusIdleGoods.Controllers
{
    /// <summary>
    /// 管理员控制器
    /// </summary>
    [RequireAdmin]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 管理员仪表板
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                PublishedProducts = await _context.Products.CountAsync(p => p.Status == ProductStatus.Published),
                PendingReview = await _context.Products.CountAsync(p => p.Status == ProductStatus.PendingReview),
                TotalCategories = await _context.Categories.CountAsync(c => c.IsEnabled),
                TotalMessages = await _context.Messages.CountAsync()
            };

            ViewBag.Stats = stats;

            // 获取待审核商品（最近10个）
            var pendingProducts = await _context.Products
                .Where(p => p.Status == ProductStatus.PendingReview)
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .OrderBy(p => p.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.PendingProducts = pendingProducts;

            return View();
        }

        /// <summary>
        /// 待审核商品列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PendingReview(int page = 1)
        {
            var pageSize = 20;
            var query = _context.Products
                .Where(p => p.Status == ProductStatus.PendingReview)
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .OrderBy(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Products = products;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View();
        }

        /// <summary>
        /// 审核商品详情
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ReviewProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            if (product.Status != ProductStatus.PendingReview)
            {
                TempData["ErrorMessage"] = "该商品不在待审核状态";
                return RedirectToAction("PendingReview");
            }

            return View(product);
        }

        /// <summary>
        /// 审核通过
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (product.Status != ProductStatus.PendingReview)
            {
                TempData["ErrorMessage"] = "该商品不在待审核状态";
                return RedirectToAction("PendingReview");
            }

            try
            {
                product.Status = ProductStatus.Published;
                product.ReviewedAt = DateTime.Now;
                product.ReviewerId = userId.Value;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员 {userId.Value} 审核通过商品 {id}");

                TempData["SuccessMessage"] = "商品审核通过，已上架！";
                return RedirectToAction("PendingReview");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审核通过商品时发生错误");
                TempData["ErrorMessage"] = "审核失败，请稍后重试";
                return RedirectToAction("ReviewProduct", new { id });
            }
        }

        /// <summary>
        /// 审核拒绝
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(int id, string rejectReason)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (product.Status != ProductStatus.PendingReview)
            {
                TempData["ErrorMessage"] = "该商品不在待审核状态";
                return RedirectToAction("PendingReview");
            }

            if (string.IsNullOrWhiteSpace(rejectReason))
            {
                TempData["ErrorMessage"] = "请填写拒绝原因";
                return RedirectToAction("ReviewProduct", new { id });
            }

            try
            {
                product.Status = ProductStatus.Rejected;
                product.RejectReason = rejectReason.Trim();
                product.ReviewedAt = DateTime.Now;
                product.ReviewerId = userId.Value;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员 {userId.Value} 拒绝商品 {id}，原因：{rejectReason}");

                TempData["SuccessMessage"] = "商品已拒绝，已通知卖家";
                return RedirectToAction("PendingReview");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "拒绝商品时发生错误");
                TempData["ErrorMessage"] = "操作失败，请稍后重试";
                return RedirectToAction("ReviewProduct", new { id });
            }
        }

        /// <summary>
        /// 批量审核通过
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken] // 暂时忽略防伪令牌，因为JSON请求需要特殊处理
        public async Task<IActionResult> BatchApprove([FromBody] int[] productIds)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            if (productIds == null || productIds.Length == 0)
            {
                return Json(new { success = false, message = "请选择要审核的商品" });
            }

            try
            {
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id) && p.Status == ProductStatus.PendingReview)
                    .ToListAsync();

                foreach (var product in products)
                {
                    product.Status = ProductStatus.Published;
                    product.ReviewedAt = DateTime.Now;
                    product.ReviewerId = userId.Value;
                    product.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员 {userId.Value} 批量审核通过 {products.Count} 个商品");

                return Json(new { success = true, message = $"成功审核通过 {products.Count} 个商品" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量审核通过时发生错误");
                return Json(new { success = false, message = "批量审核失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 审核历史记录
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ReviewHistory(int page = 1)
        {
            var pageSize = 20;
            var query = _context.Products
                .Where(p => p.Status == ProductStatus.Published || p.Status == ProductStatus.Rejected)
                .Where(p => p.ReviewedAt != null)
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.Reviewer)
                .OrderByDescending(p => p.ReviewedAt);

            var totalCount = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Products = products;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View();
        }

        #region 用户管理

        /// <summary>
        /// 用户列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Users(string search = "", string filter = "all", int page = 1)
        {
            var pageSize = 20;
            var query = _context.Users.AsQueryable();

            // 搜索
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => 
                    u.Username.Contains(search) || 
                    u.RealName.Contains(search) || 
                    u.Email.Contains(search) || 
                    u.StudentId.Contains(search));
            }

            // 筛选
            switch (filter.ToLower())
            {
                case "admin":
                    query = query.Where(u => u.IsAdmin);
                    break;
                case "verified":
                    query = query.Where(u => u.IsVerified);
                    break;
                case "enabled":
                    query = query.Where(u => u.IsEnabled);
                    break;
                case "disabled":
                    query = query.Where(u => !u.IsEnabled);
                    break;
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Users = users;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.Search = search;
            ViewBag.Filter = filter;

            return View();
        }

        /// <summary>
        /// 用户详情
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.Products)
                .Include(u => u.Favorites)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // 统计用户数据
            var stats = new
            {
                TotalProducts = user.Products.Count,
                PublishedProducts = user.Products.Count(p => p.Status == ProductStatus.Published),
                TotalFavorites = user.Favorites.Count,
                TotalMessages = await _context.Messages.CountAsync(m => m.SenderId == id || m.ReceiverId == id)
            };

            ViewBag.User = user;
            ViewBag.Stats = stats;

            return View();
        }

        /// <summary>
        /// 编辑用户（GET）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new AdminUserEditViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                RealName = user.RealName,
                StudentId = user.StudentId,
                Department = user.Department,
                PhoneNumber = user.PhoneNumber,
                WeChat = user.WeChat,
                QQ = user.QQ,
                IsVerified = user.IsVerified,
                IsEmailVerified = user.IsEmailVerified,
                IsEnabled = user.IsEnabled,
                IsAdmin = user.IsAdmin
            };

            return View(viewModel);
        }

        /// <summary>
        /// 编辑用户（POST）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(AdminUserEditViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var user = await _context.Users.FindAsync(viewModel.Id);
            if (user == null)
            {
                return NotFound();
            }

            // 检查用户名和邮箱是否已被其他用户使用
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == viewModel.Username && u.Id != viewModel.Id);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "用户名已被使用");
                return View(viewModel);
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == viewModel.Email && u.Id != viewModel.Id);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "邮箱已被使用");
                return View(viewModel);
            }

            try
            {
                user.Username = viewModel.Username;
                user.Email = viewModel.Email;
                user.RealName = viewModel.RealName;
                user.StudentId = viewModel.StudentId;
                user.Department = viewModel.Department;
                user.PhoneNumber = viewModel.PhoneNumber;
                user.WeChat = viewModel.WeChat;
                user.QQ = viewModel.QQ;
                user.IsVerified = viewModel.IsVerified;
                user.IsEmailVerified = viewModel.IsEmailVerified;
                user.IsEnabled = viewModel.IsEnabled;
                user.IsAdmin = viewModel.IsAdmin;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员编辑用户 {viewModel.Id}");

                TempData["SuccessMessage"] = "用户信息更新成功";
                return RedirectToAction("UserDetails", new { id = viewModel.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑用户时发生错误");
                TempData["ErrorMessage"] = "更新失败，请稍后重试";
                return View(viewModel);
            }
        }

        /// <summary>
        /// 切换用户启用状态
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "用户不存在" });
            }

            // 不能禁用自己
            var currentUserId = SessionHelper.GetUserId(HttpContext.Session);
            if (user.Id == currentUserId)
            {
                return Json(new { success = false, message = "不能禁用自己的账号" });
            }

            try
            {
                user.IsEnabled = !user.IsEnabled;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员{(user.IsEnabled ? "启用" : "禁用")}用户 {id}");

                return Json(new { success = true, isEnabled = user.IsEnabled, message = user.IsEnabled ? "用户已启用" : "用户已禁用" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换用户状态时发生错误");
                return Json(new { success = false, message = "操作失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 切换管理员权限
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleAdminStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "用户不存在" });
            }

            // 不能取消自己的管理员权限
            var currentUserId = SessionHelper.GetUserId(HttpContext.Session);
            if (user.Id == currentUserId)
            {
                return Json(new { success = false, message = "不能取消自己的管理员权限" });
            }

            try
            {
                user.IsAdmin = !user.IsAdmin;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员{(user.IsAdmin ? "授予" : "取消")}用户 {id} 的管理员权限");

                return Json(new { success = true, isAdmin = user.IsAdmin, message = user.IsAdmin ? "已授予管理员权限" : "已取消管理员权限" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换管理员权限时发生错误");
                return Json(new { success = false, message = "操作失败，请稍后重试" });
            }
        }

        #endregion

        #region 分类管理

        /// <summary>
        /// 分类列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .Include(c => c.Products)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // 构建树形结构
            var rootCategories = categories.Where(c => c.ParentId == null).ToList();

            ViewBag.Categories = categories;
            ViewBag.RootCategories = rootCategories;

            return View();
        }

        /// <summary>
        /// 添加分类（GET）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddCategory(int? parentId = null)
        {
            var viewModel = new AdminCategoryEditViewModel
            {
                ParentId = parentId
            };

            // 加载所有分类用于选择父分类
            var categories = await _context.Categories
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.ParentId = parentId;

            return View("EditCategory", viewModel);
        }

        /// <summary>
        /// 编辑分类（GET）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new AdminCategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ParentId = category.ParentId,
                SortOrder = category.SortOrder,
                IsEnabled = category.IsEnabled
            };

            // 加载所有分类用于选择父分类（排除自己和子分类）
            var categories = await _context.Categories
                .Where(c => c.IsEnabled && c.Id != id)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // 检查是否有子分类
            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
            if (hasChildren)
            {
                // 如果有子分类，不能选择子分类作为父分类
                var childIds = await _context.Categories
                    .Where(c => c.ParentId == id)
                    .Select(c => c.Id)
                    .ToListAsync();
                categories = categories.Where(c => !childIds.Contains(c.Id)).ToList();
            }

            ViewBag.Categories = categories;

            return View(viewModel);
        }

        /// <summary>
        /// 保存分类（POST）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(AdminCategoryEditViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // 重新加载分类列表
                var categories = await _context.Categories
                    .Where(c => c.IsEnabled && (!viewModel.Id.HasValue || c.Id != viewModel.Id.Value))
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                ViewBag.Categories = categories;
                return View("EditCategory", viewModel);
            }

            try
            {
                Category category;

                if (viewModel.Id.HasValue)
                {
                    // 编辑现有分类
                    category = await _context.Categories.FindAsync(viewModel.Id.Value);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    // 检查是否将父分类设置为自己的子分类
                    if (viewModel.ParentId.HasValue)
                    {
                        var isDescendant = await IsCategoryDescendant(viewModel.Id.Value, viewModel.ParentId.Value);
                        if (isDescendant)
                        {
                            ModelState.AddModelError("ParentId", "不能将父分类设置为自己的子分类");
                            var categories = await _context.Categories
                                .Where(c => c.IsEnabled && c.Id != viewModel.Id.Value)
                                .OrderBy(c => c.SortOrder)
                                .ThenBy(c => c.Name)
                                .ToListAsync();
                            ViewBag.Categories = categories;
                            return View("EditCategory", viewModel);
                        }
                    }
                }
                else
                {
                    // 创建新分类
                    category = new Category
                    {
                        CreatedAt = DateTime.Now
                    };
                    _context.Categories.Add(category);
                }

                // 检查分类名称是否重复（同级分类）
                var nameExists = await _context.Categories
                    .AnyAsync(c => c.Name == viewModel.Name && 
                                   c.ParentId == viewModel.ParentId && 
                                   (!viewModel.Id.HasValue || c.Id != viewModel.Id.Value));
                if (nameExists)
                {
                    ModelState.AddModelError("Name", "同级分类中已存在相同名称");
                    var categories = await _context.Categories
                        .Where(c => c.IsEnabled && (!viewModel.Id.HasValue || c.Id != viewModel.Id.Value))
                        .OrderBy(c => c.SortOrder)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                    ViewBag.Categories = categories;
                    return View("EditCategory", viewModel);
                }

                category.Name = viewModel.Name;
                category.Description = viewModel.Description;
                category.ParentId = viewModel.ParentId;
                category.SortOrder = viewModel.SortOrder;
                category.IsEnabled = viewModel.IsEnabled;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员{(viewModel.Id.HasValue ? "编辑" : "添加")}分类 {category.Id}");

                TempData["SuccessMessage"] = viewModel.Id.HasValue ? "分类更新成功" : "分类添加成功";
                return RedirectToAction("Categories");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存分类时发生错误");
                TempData["ErrorMessage"] = "操作失败，请稍后重试";
                var categories = await _context.Categories
                    .Where(c => c.IsEnabled && (!viewModel.Id.HasValue || c.Id != viewModel.Id.Value))
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                ViewBag.Categories = categories;
                return View("EditCategory", viewModel);
            }
        }

        /// <summary>
        /// 检查分类是否是另一个分类的后代
        /// </summary>
        private async Task<bool> IsCategoryDescendant(int categoryId, int ancestorId)
        {
            var current = await _context.Categories.FindAsync(categoryId);
            if (current == null) return false;

            while (current.ParentId.HasValue)
            {
                if (current.ParentId.Value == ancestorId)
                {
                    return true;
                }
                current = await _context.Categories.FindAsync(current.ParentId.Value);
                if (current == null) break;
            }

            return false;
        }

        /// <summary>
        /// 删除分类
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Children)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return Json(new { success = false, message = "分类不存在" });
            }

            // 检查是否有子分类
            if (category.Children.Any())
            {
                return Json(new { success = false, message = "该分类下有子分类，无法删除" });
            }

            // 检查是否有商品使用该分类
            if (category.Products.Any())
            {
                return Json(new { success = false, message = "该分类下有商品，无法删除" });
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员删除分类 {id}");

                return Json(new { success = true, message = "分类删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除分类时发生错误");
                return Json(new { success = false, message = "删除失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 切换分类启用状态
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "分类不存在" });
            }

            try
            {
                category.IsEnabled = !category.IsEnabled;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"管理员{(category.IsEnabled ? "启用" : "禁用")}分类 {id}");

                return Json(new { success = true, isEnabled = category.IsEnabled, message = category.IsEnabled ? "分类已启用" : "分类已禁用" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换分类状态时发生错误");
                return Json(new { success = false, message = "操作失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 获取分类JSON（用于AJAX）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategoriesJson()
        {
            var categories = await _context.Categories
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.ParentId,
                    c.SortOrder
                })
                .ToListAsync();

            return Json(categories);
        }

        #endregion
    }
}

