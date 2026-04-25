using CampusIdleGoods.Data;
using CampusIdleGoods.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CampusIdleGoods.Services
{
    /// <summary>
    /// 认证服务实现
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            ApplicationDbContext context, 
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
                return false;

            // 使用BCrypt或简单的SHA256（这里使用SHA256，生产环境建议使用BCrypt）
            var hash = HashPassword(password);
            return hash == passwordHash;
        }

        /// <summary>
        /// 生成密码哈希（使用SHA256）
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("密码不能为空", nameof(password));

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// 根据用户名或邮箱查找用户
        /// </summary>
        public async Task<User?> FindUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            if (string.IsNullOrEmpty(usernameOrEmail))
                return null;

            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task<User> CreateUserAsync(User user, string password)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("密码不能为空", nameof(password));

            // 检查用户名和邮箱是否已存在
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == user.Username || u.Email == user.Email || u.StudentId == user.StudentId);

            if (existingUser != null)
            {
                if (existingUser.Username == user.Username)
                    throw new InvalidOperationException("用户名已存在");
                if (existingUser.Email == user.Email)
                    throw new InvalidOperationException("邮箱已被注册");
                if (existingUser.StudentId == user.StudentId)
                    throw new InvalidOperationException("学号已被注册");
            }

            // 设置密码哈希
            user.PasswordHash = HashPassword(password);
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// 验证学号格式（示例：支持常见的学号格式，可根据实际需求调整）
        /// </summary>
        public bool ValidateStudentId(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
                return false;

            // 学号格式：通常为数字，长度在6-20位之间
            // 可以根据实际学校的学号格式调整正则表达式
            var pattern = @"^\d{6,20}$";
            return Regex.IsMatch(studentId, pattern);
        }

        /// <summary>
        /// 生成邮箱验证令牌
        /// </summary>
        public string GenerateEmailVerificationToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        public async Task<bool> SendVerificationEmailAsync(string email, string token)
        {
            try
            {
                var senderEmail = _configuration["Email:SenderEmail"] ?? "yangbodi923@gmail.com";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "";
                var senderName = _configuration["Email:SenderName"] ?? "校园闲置好物阁";
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";

                // 构建验证链接
                var verificationUrl = $"{baseUrl}/Account/VerifyEmail?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

                // 构建MIME邮件
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = "【校园闲置好物阁】邮箱验证";

                // 构建邮件内容（HTML格式）
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #333;'>欢迎注册校园闲置好物阁！</h2>
                            <p>感谢您注册我们的平台。请点击下面的按钮验证您的邮箱地址：</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{verificationUrl}' 
                                   style='background-color: #007bff; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    验证邮箱
                                </a>
                            </div>
                            <p style='color: #666; font-size: 14px;'>
                                如果按钮无法点击，请复制以下链接到浏览器中打开：<br/>
                                <a href='{verificationUrl}'>{verificationUrl}</a>
                            </p>
                            <p style='color: #666; font-size: 14px;'>
                                此验证链接将在24小时后失效。
                            </p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'/>
                            <p style='color: #999; font-size: 12px;'>
                                这是一封自动发送的邮件，请勿回复。<br/>
                                如果您没有注册此账户，请忽略此邮件。
                            </p>
                        </div>",
                    TextBody = $"欢迎注册校园闲置好物阁！请复制以下链接到浏览器中验证您的邮箱：{verificationUrl}\n此验证链接将在24小时后失效。"
                };
                message.Body = bodyBuilder.ToMessageBody();

                // 配置SMTP客户端并发送
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"邮箱验证邮件已发送至 {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送邮箱验证邮件失败: {email}");
                return false;
            }
        }

        /// <summary>
        /// 验证邮箱验证令牌
        /// </summary>
        public async Task<bool> VerifyEmailTokenAsync(string email, string token)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                    return false;

                // 检查令牌是否匹配
                if (user.EmailVerificationToken != token)
                    return false;

                // 检查令牌是否过期
                if (user.EmailVerificationTokenExpires == null || 
                    user.EmailVerificationTokenExpires < DateTime.Now)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证邮箱令牌时发生错误: {email}");
                return false;
            }
        }

        /// <summary>
        /// 发送密码重置邮件
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string email, string token)
        {
            try
            {
                var senderEmail = _configuration["Email:SenderEmail"] ?? "yangbodi923@gmail.com";
                var senderPassword = _configuration["Email:SenderPassword"] ?? "";
                var senderName = _configuration["Email:SenderName"] ?? "校园闲置好物阁";
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";

                // 构建重置链接
                var resetUrl = $"{baseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

                // 构建MIME邮件
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = "【校园闲置好物阁】密码重置";

                // 构建邮件内容（HTML格式）
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #333;'>密码重置请求</h2>
                            <p>您好，</p>
                            <p>我们收到了您的密码重置请求。请点击下面的按钮重置您的密码：</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetUrl}' 
                                   style='background-color: #dc3545; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    重置密码
                                </a>
                            </div>
                            <p style='color: #666; font-size: 14px;'>
                                如果按钮无法点击，请复制以下链接到浏览器中打开：<br/>
                                <a href='{resetUrl}'>{resetUrl}</a>
                            </p>
                            <p style='color: #666; font-size: 14px;'>
                                此重置链接将在1小时后失效。
                            </p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'/>
                            <p style='color: #999; font-size: 12px;'>
                                这是一封自动发送的邮件，请勿回复。<br/>
                                如果您没有请求重置密码，请忽略此邮件。
                            </p>
                        </div>",
                    TextBody = $"您的密码重置请求已收到。请复制以下链接到浏览器中重置密码：{resetUrl}\n此重置链接将在1小时后失效。"
                };
                message.Body = bodyBuilder.ToMessageBody();

                // 配置SMTP客户端并发送
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"密码重置邮件已发送至 {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送密码重置邮件失败: {email}");
                return false;
            }
        }

        /// <summary>
        /// 验证密码重置令牌
        /// </summary>
        public async Task<bool> VerifyPasswordResetTokenAsync(string email, string token)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                    return false;

                // 检查令牌是否匹配
                if (user.PasswordResetToken != token)
                    return false;

                // 检查令牌是否过期
                if (user.PasswordResetTokenExpires == null || 
                    user.PasswordResetTokenExpires < DateTime.Now)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证密码重置令牌时发生错误: {email}");
                return false;
            }
        }
    }
}

