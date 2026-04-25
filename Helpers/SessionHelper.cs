using CampusIdleGoods.Models;

namespace CampusIdleGoods.Helpers
{
    /// <summary>
    /// Session辅助类
    /// </summary>
    public static class SessionHelper
    {
        private const string UserIdKey = "UserId";
        private const string UsernameKey = "Username";
        private const string IsAdminKey = "IsAdmin";
        private const string IsVerifiedKey = "IsVerified";

        /// <summary>
        /// 设置用户登录信息到Session
        /// </summary>
        public static void SetUserSession(ISession session, User user)
        {
            session.SetInt32(UserIdKey, user.Id);
            session.SetString(UsernameKey, user.Username);
            session.SetString(IsAdminKey, user.IsAdmin.ToString());
            session.SetString(IsVerifiedKey, user.IsVerified.ToString());
        }

        /// <summary>
        /// 获取当前登录用户ID
        /// </summary>
        public static int? GetUserId(ISession session)
        {
            return session.GetInt32(UserIdKey);
        }

        /// <summary>
        /// 获取当前登录用户名
        /// </summary>
        public static string? GetUsername(ISession session)
        {
            return session.GetString(UsernameKey);
        }

        /// <summary>
        /// 检查用户是否已登录
        /// </summary>
        public static bool IsLoggedIn(ISession session)
        {
            return GetUserId(session).HasValue;
        }

        /// <summary>
        /// 检查用户是否为管理员
        /// </summary>
        public static bool IsAdmin(ISession session)
        {
            var isAdminStr = session.GetString(IsAdminKey);
            return bool.TryParse(isAdminStr, out var isAdmin) && isAdmin;
        }

        /// <summary>
        /// 检查用户是否已认证
        /// </summary>
        public static bool IsVerified(ISession session)
        {
            var isVerifiedStr = session.GetString(IsVerifiedKey);
            return bool.TryParse(isVerifiedStr, out var isVerified) && isVerified;
        }

        /// <summary>
        /// 清除用户Session
        /// </summary>
        public static void ClearSession(ISession session)
        {
            session.Clear();
        }
    }
}

