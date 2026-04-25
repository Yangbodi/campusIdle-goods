using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 编辑个人资料视图模型
    /// </summary>
    public class EditProfileViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "邮箱不能为空")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [Display(Name = "真实姓名")]
        public string RealName { get; set; } = string.Empty;

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

        [Display(Name = "头像路径")]
        public string? AvatarPath { get; set; }
    }
}

