using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 重置密码视图模型
    /// </summary>
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "邮箱不能为空")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "确认密码不能为空")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [Display(Name = "确认密码")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}

