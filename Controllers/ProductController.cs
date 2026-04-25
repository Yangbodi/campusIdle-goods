using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusIdleGoods.Models;
using CampusIdleGoods.Models.ViewModels;
using CampusIdleGoods.Data;
using CampusIdleGoods.Helpers;
using CampusIdleGoods.Attributes;

namespace CampusIdleGoods.Controllers
{
    /// <summary>
    /// 商品控制器
    /// </summary>
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            ApplicationDbContext context,
            ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 发布商品页面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create") });
            }

            // 检查用户邮箱是否已验证
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!user.IsEmailVerified)
            {
                TempData["ErrorMessage"] = "您需要先验证邮箱才能发布商品。请查收邮箱验证邮件，或者在个人中心重新发送验证邮件。";
                return RedirectToAction("Profile", "Account");
            }

            var categories = await _context.Categories
                .Where(c => c.IsEnabled && c.ParentId == null)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            ViewBag.Categories = categories;
            return View();
        }

        /// <summary>
        /// 处理发布商品请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model, List<IFormFile> images)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // 检查用户邮箱是否已验证
            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!user.IsEmailVerified)
            {
                TempData["ErrorMessage"] = "您需要先验证邮箱才能发布商品。";
                return RedirectToAction("Profile", "Account");
            }

            if (!ModelState.IsValid)
            {
                var categories = await _context.Categories
                    .Where(c => c.IsEnabled && c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .ToListAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            try
            {
                // 验证分类是否存在
                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category == null || !category.IsEnabled)
                {
                    ModelState.AddModelError("CategoryId", "选择的分类不存在或已禁用");
                    var categories = await _context.Categories
                        .Where(c => c.IsEnabled && c.ParentId == null)
                        .OrderBy(c => c.SortOrder)
                        .ToListAsync();
                    ViewBag.Categories = categories;
                    return View(model);
                }

                // 创建商品
                var product = new Product
                {
                    Title = model.Title,
                    Description = model.Description,
                    Price = model.Price,
                    OriginalPrice = model.OriginalPrice,
                    CategoryId = model.CategoryId,
                    SellerId = userId.Value,
                    PickupLocation = model.PickupLocation,
                    IsNegotiable = model.IsNegotiable,
                    Status = model.SaveAsDraft ? ProductStatus.Draft : ProductStatus.PendingReview,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // 处理标签
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    var tagNames = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .Distinct()
                        .Take(10); // 最多10个标签

                    foreach (var tagName in tagNames)
                    {
                        var tag = new ProductTag
                        {
                            ProductId = product.Id,
                            TagName = tagName,
                            CreatedAt = DateTime.Now
                        };
                        _context.ProductTags.Add(tag);
                    }
                }

                // 处理图片上传
                if (images != null && images.Count > 0)
                {
                    var uploadsFolder = Path.Combine("wwwroot", "uploads", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var imageCount = 0;
                    foreach (var image in images.Take(5)) // 最多5张
                    {
                        if (image.Length > 0 && image.Length <= 5 * 1024 * 1024) // 5MB限制
                        {
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                            if (allowedExtensions.Contains(fileExtension))
                            {
                                var fileName = $"{product.Id}_{DateTime.Now:yyyyMMddHHmmss}_{imageCount}{fileExtension}";
                                var filePath = Path.Combine(uploadsFolder, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }

                                var productImage = new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImagePath = $"/uploads/products/{fileName}",
                                    SortOrder = imageCount,
                                    CreatedAt = DateTime.Now
                                };
                                _context.ProductImages.Add(productImage);
                                imageCount++;
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (model.SaveAsDraft)
                {
                    TempData["SuccessMessage"] = "商品已保存为草稿！";
                    return RedirectToAction("MyProducts");
                }
                else
                {
                    TempData["SuccessMessage"] = "商品发布成功！等待管理员审核。";
                    return RedirectToAction("MyProducts");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布商品时发生错误");
                ModelState.AddModelError("", "发布失败，请稍后重试");
                var categories = await _context.Categories
                    .Where(c => c.IsEnabled && c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .ToListAsync();
                ViewBag.Categories = categories;
                return View(model);
            }
        }

        /// <summary>
        /// 我的商品列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyProducts()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var products = await _context.Products
                .Where(p => p.SellerId == userId.Value)
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        /// <summary>
        /// 商品详情
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
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

            // 增加浏览次数
            product.ViewCount++;
            await _context.SaveChangesAsync();

            // 检查是否已收藏
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (userId.HasValue)
            {
                var isFavorited = await _context.Favorites
                    .AnyAsync(f => f.UserId == userId.Value && f.ProductId == id);
                ViewBag.IsFavorited = isFavorited;
            }

            return View(product);
        }

        /// <summary>
        /// 编辑商品页面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId.Value);

            if (product == null)
            {
                return NotFound();
            }

            // 只有草稿或待审核状态的商品可以编辑
            if (product.Status != ProductStatus.Draft && product.Status != ProductStatus.PendingReview)
            {
                TempData["ErrorMessage"] = "该商品状态不允许编辑";
                return RedirectToAction("MyProducts");
            }

            var model = new EditProductViewModel
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                CategoryId = product.CategoryId,
                PickupLocation = product.PickupLocation,
                IsNegotiable = product.IsNegotiable,
                Tags = string.Join(", ", product.Tags.Select(t => t.TagName)),
                Status = product.Status
            };

            var categories = await _context.Categories
                .Where(c => c.IsEnabled && c.ParentId == null)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.ProductImages = product.Images.OrderBy(i => i.SortOrder).ToList();

            return View(model);
        }

        /// <summary>
        /// 处理编辑商品请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProductViewModel model, List<IFormFile> newImages)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                var categories = await _context.Categories
                    .Where(c => c.IsEnabled && c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .ToListAsync();
                ViewBag.Categories = categories;
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == model.Id);
                if (product != null)
                {
                    ViewBag.ProductImages = product.Images.OrderBy(i => i.SortOrder).ToList();
                }
                return View(model);
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .Include(p => p.Tags)
                    .FirstOrDefaultAsync(p => p.Id == model.Id && p.SellerId == userId.Value);

                if (product == null)
                {
                    return NotFound();
                }

                // 更新商品信息
                product.Title = model.Title;
                product.Description = model.Description;
                product.Price = model.Price;
                product.OriginalPrice = model.OriginalPrice;
                product.CategoryId = model.CategoryId;
                product.PickupLocation = model.PickupLocation;
                product.IsNegotiable = model.IsNegotiable;
                product.UpdatedAt = DateTime.Now;

                // 如果之前是待审核状态，编辑后重新变为待审核
                if (product.Status == ProductStatus.PendingReview)
                {
                    product.Status = ProductStatus.PendingReview;
                }

                // 更新标签
                _context.ProductTags.RemoveRange(product.Tags);
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    var tagNames = model.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .Distinct()
                        .Take(10);

                    foreach (var tagName in tagNames)
                    {
                        var tag = new ProductTag
                        {
                            ProductId = product.Id,
                            TagName = tagName,
                            CreatedAt = DateTime.Now
                        };
                        _context.ProductTags.Add(tag);
                    }
                }

                // 处理新上传的图片
                if (newImages != null && newImages.Count > 0)
                {
                    var existingImageCount = product.Images.Count;
                    var maxImages = 5;
                    var remainingSlots = maxImages - existingImageCount;

                    if (remainingSlots > 0)
                    {
                        var uploadsFolder = Path.Combine("wwwroot", "uploads", "products");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var imageCount = existingImageCount;
                        foreach (var image in newImages.Take(remainingSlots))
                        {
                            if (image.Length > 0 && image.Length <= 5 * 1024 * 1024)
                            {
                                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                                var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                                if (allowedExtensions.Contains(fileExtension))
                                {
                                    var fileName = $"{product.Id}_{DateTime.Now:yyyyMMddHHmmss}_{imageCount}{fileExtension}";
                                    var filePath = Path.Combine(uploadsFolder, fileName);

                                    using (var stream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await image.CopyToAsync(stream);
                                    }

                                    var productImage = new ProductImage
                                    {
                                        ProductId = product.Id,
                                        ImagePath = $"/uploads/products/{fileName}",
                                        SortOrder = imageCount,
                                        CreatedAt = DateTime.Now
                                    };
                                    _context.ProductImages.Add(productImage);
                                    imageCount++;
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "商品信息更新成功！";
                return RedirectToAction("MyProducts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑商品时发生错误");
                ModelState.AddModelError("", "更新失败，请稍后重试");
                var categories = await _context.Categories
                    .Where(c => c.IsEnabled && c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .ToListAsync();
                ViewBag.Categories = categories;
                return View(model);
            }
        }

        /// <summary>
        /// 删除商品图片
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var image = await _context.ProductImages
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == imageId);

            if (image == null || image.Product.SellerId != userId.Value)
            {
                return Json(new { success = false, message = "图片不存在或无权限" });
            }

            try
            {
                // 删除文件
                var filePath = Path.Combine("wwwroot", image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "图片删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除图片时发生错误");
                return Json(new { success = false, message = "删除失败" });
            }
        }

        /// <summary>
        /// 删除商品
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .Include(p => p.Favorites)
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId.Value);

            if (product == null)
            {
                return NotFound();
            }

            try
            {
                // 删除商品图片文件
                foreach (var image in product.Images)
                {
                    var filePath = Path.Combine("wwwroot", image.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "商品删除成功！";
                return RedirectToAction("MyProducts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除商品时发生错误");
                TempData["ErrorMessage"] = "删除失败，请稍后重试";
                return RedirectToAction("MyProducts");
            }
        }

        /// <summary>
        /// 下架商品
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId.Value);

            if (product == null)
            {
                return NotFound();
            }

            if (product.Status == ProductStatus.Published)
            {
                product.Status = ProductStatus.Removed;
                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "商品已下架！";
            }

            return RedirectToAction("MyProducts");
        }

        /// <summary>
        /// 重新上架商品
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Republish(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId.Value);

            if (product == null)
            {
                return NotFound();
            }

            if (product.Status == ProductStatus.Removed)
            {
                product.Status = ProductStatus.Published;
                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "商品已重新上架！";
            }

            return RedirectToAction("MyProducts");
        }

        /// <summary>
        /// 标记为已售出
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.SellerId == userId.Value);

            if (product == null)
            {
                return NotFound();
            }

            if (product.Status == ProductStatus.Published)
            {
                product.Status = ProductStatus.Sold;
                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "商品已标记为已售出！";
            }

            return RedirectToAction("MyProducts");
        }

        /// <summary>
        /// 获取子分类（AJAX）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSubCategories(int parentId)
        {
            var subCategories = await _context.Categories
                .Where(c => c.ParentId == parentId && c.IsEnabled)
                .OrderBy(c => c.SortOrder)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return Json(subCategories);
        }

        /// <summary>
        /// 商品浏览列表（支持搜索和筛选）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Browse(ProductSearchViewModel model)
        {
            var query = _context.Products
                .Where(p => p.Status == ProductStatus.Published)
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .Include(p => p.Seller)
                .AsQueryable();

            // 关键词搜索（标题、描述、标签）
            if (!string.IsNullOrEmpty(model.Keyword))
            {
                var keyword = model.Keyword.Trim();
                
                // 标签搜索需要单独处理
                var productsWithTag = await _context.ProductTags
                    .Where(t => t.TagName.Contains(keyword))
                    .Select(t => t.ProductId)
                    .Distinct()
                    .ToListAsync();
                
                if (productsWithTag.Any())
                {
                    query = query.Where(p => 
                        p.Title.Contains(keyword) ||
                        p.Description.Contains(keyword) ||
                        productsWithTag.Contains(p.Id));
                }
                else
                {
                    query = query.Where(p =>
                        p.Title.Contains(keyword) ||
                        p.Description.Contains(keyword));
                }
            }

            // 分类筛选
            if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == model.CategoryId.Value);
            }

            // 价格筛选
            if (model.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= model.MinPrice.Value);
            }
            if (model.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= model.MaxPrice.Value);
            }

            // 时间筛选
            if (!string.IsNullOrEmpty(model.TimeFilter))
            {
                var now = DateTime.Now;
                switch (model.TimeFilter)
                {
                    case "24h":
                        query = query.Where(p => p.CreatedAt >= now.AddHours(-24));
                        break;
                    case "7d":
                        query = query.Where(p => p.CreatedAt >= now.AddDays(-7));
                        break;
                    case "30d":
                        query = query.Where(p => p.CreatedAt >= now.AddDays(-30));
                        break;
                }
            }

            // 排序
            switch (model.SortBy)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "views":
                    query = query.OrderByDescending(p => p.ViewCount);
                    break;
                case "latest":
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // 分页
            model.TotalCount = await query.CountAsync();
            var products = await query
                .Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            // 获取分类列表（用于筛选）
            var categories = await _context.Categories
                .Where(c => c.IsEnabled && c.ParentId == null)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Products = products;

            return View(model);
        }

        /// <summary>
        /// 按分类浏览
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Category(int id, int page = 1)
        {
            var category = await _context.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsEnabled);

            if (category == null)
            {
                return NotFound();
            }

            // 获取该分类及其子分类的所有商品
            var categoryIds = new List<int> { category.Id };
            if (category.Children != null && category.Children.Any())
            {
                categoryIds.AddRange(category.Children.Where(c => c.IsEnabled).Select(c => c.Id));
            }

            var pageSize = 12;
            var query = _context.Products
                .Where(p => p.Status == ProductStatus.Published && categoryIds.Contains(p.CategoryId))
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .Include(p => p.Seller)
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Category = category;
            ViewBag.Products = products;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View();
        }
    }
}

