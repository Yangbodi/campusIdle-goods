using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 管理员编辑分类视图模型
    /// </summary>
    public class AdminCategoryEditViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "分类名称不能为空")]
        [StringLength(50, ErrorMessage = "分类名称长度不能超过50个字符")]
        [Display(Name = "分类名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "分类描述长度不能超过200个字符")]
        [Display(Name = "分类描述")]
        public string? Description { get; set; }

        [Display(Name = "父分类")]
        public int? ParentId { get; set; }

        [Display(Name = "排序顺序")]
        [Range(0, int.MaxValue, ErrorMessage = "排序顺序必须大于等于0")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "是否启用")]
        public bool IsEnabled { get; set; } = true;
    }
}

