using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusIdleGoods.Models;
using CampusIdleGoods.Models.ViewModels;
using CampusIdleGoods.Services;
using CampusIdleGoods.Data;
using CampusIdleGoods.Helpers;

namespace CampusIdleGoods.Controllers
{
    /// <summary>
    /// 账户控制器
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 注册页面
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // 如果已登录，重定向到首页
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /// <summary>
        /// 处理注册请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 验证学号格式
            if (!_authService.ValidateStudentId(model.StudentId))
            {
                ModelState.AddModelError("StudentId", "学号格式不正确，请输入6-20位数字");
                return View(model);
            }

            try
            {
                // 创建用户对象
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    RealName = model.RealName,
                    StudentId = model.StudentId,
                    Department = model.Department,
                    PhoneNumber = model.PhoneNumber,
                    WeChat = model.WeChat,
                    QQ = model.QQ,
                    IsVerified = false,
                    IsEmailVerified = false,
                    IsEnabled = true,
                    IsAdmin = false
                };

                // 创建用户
                await _authService.CreateUserAsync(user, model.Password);

                // 生成邮箱验证令牌并保存到数据库
                var token = _authService.GenerateEmailVerificationToken();
                user.EmailVerificationToken = token;
                user.EmailVerificationTokenExpires = DateTime.Now.AddHours(24); // 24小时后过期
                await _context.SaveChangesAsync();

                // 发送验证邮件
                await _authService.SendVerificationEmailAsync(user.Email, token);

                TempData["SuccessMessage"] = "注册成功！我们已向您的邮箱发送了验证邮件，请在24小时内完成验证。";
                return RedirectToAction("Login");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册用户时发生错误");
                ModelState.AddModelError("", "注册失败，请稍后重试");
                return View(model);
            }
        }

        /// <summary>
        /// 登录页面
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // 如果已登录，重定向到首页或返回URL
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                return Redirect(returnUrl ?? Url.Action("Index", "Home") ?? "/");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// 处理登录请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 查找用户
                var user = await _authService.FindUserByUsernameOrEmailAsync(model.UsernameOrEmail);

                if (user == null)
                {
                    ModelState.AddModelError("", "用户名或密码错误");
                    return View(model);
                }

                // 验证密码
                if (!_authService.VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "用户名或密码错误");
                    return View(model);
                }

                // 检查用户是否被禁用
                if (!user.IsEnabled)
                {
                    ModelState.AddModelError("", "账户已被禁用，请联系管理员");
                    return View(model);
                }

                // 设置Session
                SessionHelper.SetUserSession(HttpContext.Session, user);

                _logger.LogInformation($"用户 {user.Username} 登录成功");

                // 重定向到返回URL或首页
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录时发生错误");
                ModelState.AddModelError("", "登录失败，请稍后重试");
                return View(model);
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var username = SessionHelper.GetUsername(HttpContext.Session);
            SessionHelper.ClearSession(HttpContext.Session);

            if (!string.IsNullOrEmpty(username))
            {
                _logger.LogInformation($"用户 {username} 已登出");
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// 忘记密码页面
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// 处理忘记密码请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    // 为了安全，不提示用户是否存在
                    TempData["SuccessMessage"] = "如果该邮箱已注册，我们已发送密码重置链接到您的邮箱。";
                    return RedirectToAction("Login");
                }

                // 生成重置令牌并保存到数据库
                var token = _authService.GenerateEmailVerificationToken();
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpires = DateTime.Now.AddHours(1); // 1小时后过期
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // 发送密码重置邮件
                var isStatus = await _authService.SendPasswordResetEmailAsync(user.Email, token);

                if (!isStatus)
                {
                    TempData["ErrorMessage"] = "邮箱发送失败，请稍微再试！";

                    return RedirectToAction("ForgotPassword");
                }
                else
                {
                    _logger.LogInformation($"用户 {user.Username} 请求重置密码");

                    TempData["SuccessMessage"] = "如果该邮箱已注册，我们已发送密码重置链接到您的邮箱。";
                    return RedirectToAction("Login");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理忘记密码请求时发生错误");
                ModelState.AddModelError("", "处理请求失败，请稍后重试");
                return View(model);
            }
        }

        /// <summary>
        /// 重置密码页面
        /// </summary>
        [HttpGet]
        public IActionResult ResetPassword(string? email = null, string? token = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        /// <summary>
        /// 处理重置密码请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "无效的请求");
                    return View(model);
                }

                // 验证重置令牌
                var isValid = await _authService.VerifyPasswordResetTokenAsync(model.Email, model.Token);
                if (!isValid)
                {
                    ModelState.AddModelError("", "重置链接无效或已过期，请重新申请密码重置");
                    return View(model);
                }

                // 更新密码并清除重置令牌
                user.PasswordHash = _authService.HashPassword(model.Password);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpires = null;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"用户 {user.Username} 重置密码成功");

                TempData["SuccessMessage"] = "密码重置成功，请使用新密码登录。";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置密码时发生错误");
                ModelState.AddModelError("", "重置密码失败，请稍后重试");
                return View(model);
            }
        }

        /// <summary>
        /// 个人中心
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Profile") });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                SessionHelper.ClearSession(HttpContext.Session);
                return RedirectToAction("Login");
            }

            return View(user);
        }

        /// <summary>
        /// 编辑个人资料页面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("EditProfile") });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                SessionHelper.ClearSession(HttpContext.Session);
                return RedirectToAction("Login");
            }

            var model = new EditProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                RealName = user.RealName,
                Department = user.Department,
                PhoneNumber = user.PhoneNumber,
                WeChat = user.WeChat,
                QQ = user.QQ,
                AvatarPath = user.AvatarPath
            };

            return View(model);
        }

        /// <summary>
        /// 处理编辑个人资料请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue || userId.Value != model.Id)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users.FindAsync(model.Id);
                if (user == null)
                {
                    ModelState.AddModelError("", "用户不存在");
                    return View(model);
                }

                // 检查邮箱是否被其他用户使用
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email && u.Id != model.Id);
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "该邮箱已被其他用户使用");
                        return View(model);
                    }
                }

                // 更新用户信息
                user.Email = model.Email;
                user.RealName = model.RealName;
                user.Department = model.Department;
                user.PhoneNumber = model.PhoneNumber;
                user.WeChat = model.WeChat;
                user.QQ = model.QQ;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "个人信息更新成功！";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新个人信息时发生错误");
                ModelState.AddModelError("", "更新失败，请稍后重试");
                return View(model);
            }
        }

        /// <summary>
        /// 修改密码页面
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("ChangePassword") });
            }

            return View();
        }

        /// <summary>
        /// 处理修改密码请求
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    SessionHelper.ClearSession(HttpContext.Session);
                    return RedirectToAction("Login");
                }

                // 验证当前密码
                if (!_authService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "当前密码不正确");
                    return View(model);
                }

                // 更新密码
                user.PasswordHash = _authService.HashPassword(model.NewPassword);
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"用户 {user.Username} 修改密码成功");

                TempData["SuccessMessage"] = "密码修改成功！";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密码时发生错误");
                ModelState.AddModelError("", "修改密码失败，请稍后重试");
                return View(model);
            }
        }

        /// <summary>
        /// 上传头像
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            if (avatarFile == null || avatarFile.Length == 0)
            {
                return Json(new { success = false, message = "请选择要上传的文件" });
            }

            // 验证文件类型
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, message = "只支持上传 jpg、jpeg、png、gif 格式的图片" });
            }

            // 验证文件大小（限制为5MB）
            if (avatarFile.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "图片大小不能超过5MB" });
            }

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "用户不存在" });
                }

                // 创建上传目录
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 生成唯一文件名
                var fileName = $"{userId.Value}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                // 删除旧头像（如果存在）
                if (!string.IsNullOrEmpty(user.AvatarPath))
                {
                    var oldFilePath = Path.Combine("wwwroot", user.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // 更新用户头像路径
                user.AvatarPath = $"/uploads/avatars/{fileName}";
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "头像上传成功", avatarPath = user.AvatarPath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传头像时发生错误");
                return Json(new { success = false, message = "上传失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 邮箱验证
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string? email = null, string? token = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "验证链接无效";
                return RedirectToAction("Login");
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "用户不存在";
                    return RedirectToAction("Login");
                }

                // 检查邮箱是否已验证
                if (user.IsEmailVerified)
                {
                    TempData["InfoMessage"] = "您的邮箱已经验证过了";
                    return RedirectToAction("Login");
                }

                // 验证令牌
                var isValid = await _authService.VerifyEmailTokenAsync(email, token);
                if (!isValid)
                {
                    TempData["ErrorMessage"] = "验证链接无效或已过期，请重新发送验证邮件";
                    return RedirectToAction("Login");
                }

                // 验证成功，更新用户状态
                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpires = null;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"用户 {user.Username} 邮箱验证成功");

                TempData["SuccessMessage"] = "邮箱验证成功！现在您可以发布商品了。";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "邮箱验证时发生错误");
                TempData["ErrorMessage"] = "验证失败，请稍后重试";
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// 重新发送验证邮件
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "用户不存在" });
                }

                if (user.IsEmailVerified)
                {
                    return Json(new { success = false, message = "您的邮箱已经验证过了" });
                }

                // 生成新的验证令牌
                var token = _authService.GenerateEmailVerificationToken();
                user.EmailVerificationToken = token;
                user.EmailVerificationTokenExpires = DateTime.Now.AddHours(24);
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // 发送验证邮件
                var emailSent = await _authService.SendVerificationEmailAsync(user.Email, token);
                if (!emailSent)
                {
                    return Json(new { success = false, message = "邮件发送失败，请稍后重试" });
                }

                _logger.LogInformation($"用户 {user.Username} 重新发送了验证邮件");

                return Json(new { success = true, message = "验证邮件已发送，请查收邮箱" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新发送验证邮件时发生错误");
                return Json(new { success = false, message = "发送失败，请稍后重试" });
            }
        }
    }
}