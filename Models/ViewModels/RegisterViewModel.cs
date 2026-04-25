using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 注册视图模型
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50个字符之间")]
        [Display(Name = "用户名")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "确认密码不能为空")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [Display(Name = "确认密码")]
        public string ConfirmPassword { get; set; } = string.Empty;

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
    }
}

