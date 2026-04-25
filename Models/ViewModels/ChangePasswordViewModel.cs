using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 修改密码视图模型
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "当前密码不能为空")]
        [DataType(DataType.Password)]
        [Display(Name = "当前密码")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "确认密码不能为空")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
        [Display(Name = "确认新密码")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

