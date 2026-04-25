using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 登录视图模型
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "用户名或邮箱不能为空")]
        [Display(Name = "用户名或邮箱")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "密码不能为空")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "记住我")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

