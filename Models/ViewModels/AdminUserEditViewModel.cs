using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 管理员编辑用户视图模型
    /// </summary>
    public class AdminUserEditViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
        [Display(Name = "用户名")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "邮箱不能为空")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [Display(Name = "真实姓名")]
        public string RealName { get; set; } = string.Empty;

        [Required(ErrorMessage = "学号不能为空")]
        [StringLength(20, ErrorMessage = "学号长度不能超过20个字符")]
        [Display(Name = "学号")]
        public string StudentId { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "院系专业长度不能超过100个字符")]
        [Display(Name = "院系专业")]
        public string? Department { get; set; }

        [Phone(ErrorMessage = "手机号格式不正确")]
        [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
        [Display(Name = "手机号")]
        public string? PhoneNumber { get; set; }

        [StringLength(50, ErrorMessage = "微信长度不能超过50个字符")]
        [Display(Name = "微信")]
        public string? WeChat { get; set; }

        [StringLength(50, ErrorMessage = "QQ长度不能超过50个字符")]
        [Display(Name = "QQ")]
        public string? QQ { get; set; }

        [Display(Name = "是否已认证")]
        public bool IsVerified { get; set; }

        [Display(Name = "是否邮箱已验证")]
        public bool IsEmailVerified { get; set; }

        [Display(Name = "是否启用")]
        public bool IsEnabled { get; set; }

        [Display(Name = "是否为管理员")]
        public bool IsAdmin { get; set; }
    }
}

