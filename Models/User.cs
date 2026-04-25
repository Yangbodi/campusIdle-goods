using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models
{
    /// <summary>
    /// 用户模型
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "用户名")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Display(Name = "密码哈希")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "真实姓名")]
        public string RealName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "学号")]
        public string StudentId { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "院系专业")]
        public string? Department { get; set; }

        [StringLength(20)]
        [Display(Name = "手机号")]
        public string? PhoneNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "微信")]
        public string? WeChat { get; set; }

        [StringLength(50)]
        [Display(Name = "QQ")]
        public string? QQ { get; set; }

        [StringLength(255)]
        [Display(Name = "头像路径")]
        public string? AvatarPath { get; set; }

        [Display(Name = "是否已认证")]
        public bool IsVerified { get; set; } = false;

        [Display(Name = "是否邮箱已验证")]
        public bool IsEmailVerified { get; set; } = false;

        [StringLength(255)]
        [Display(Name = "邮箱验证令牌")]
        public string? EmailVerificationToken { get; set; }

        [Display(Name = "邮箱验证令牌过期时间")]
        public DateTime? EmailVerificationTokenExpires { get; set; }

        [StringLength(255)]
        [Display(Name = "密码重置令牌")]
        public string? PasswordResetToken { get; set; }

        [Display(Name = "密码重置令牌过期时间")]
        public DateTime? PasswordResetTokenExpires { get; set; }

        [Display(Name = "是否启用")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "是否为管理员")]
        public bool IsAdmin { get; set; } = false;

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}

