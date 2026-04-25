using CampusIdleGoods.Models;

namespace CampusIdleGoods.Services
{
    /// <summary>
    /// 认证服务接口
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 验证密码
        /// </summary>
        bool VerifyPassword(string password, string passwordHash);

        /// <summary>
        /// 生成密码哈希
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// 根据用户名或邮箱查找用户
        /// </summary>
        Task<User?> FindUserByUsernameOrEmailAsync(string usernameOrEmail);

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<User> CreateUserAsync(User user, string password);

        /// <summary>
        /// 验证学号格式
        /// </summary>
        bool ValidateStudentId(string studentId);

        /// <summary>
        /// 生成邮箱验证令牌
        /// </summary>
        string GenerateEmailVerificationToken();

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        Task<bool> SendVerificationEmailAsync(string email, string token);

        /// <summary>
        /// 验证邮箱验证令牌
        /// </summary>
        Task<bool> VerifyEmailTokenAsync(string email, string token);

        /// <summary>
        /// 发送密码重置邮件
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(string email, string token);

        /// <summary>
        /// 验证密码重置令牌
        /// </summary>
        Task<bool> VerifyPasswordResetTokenAsync(string email, string token);
    }
}

