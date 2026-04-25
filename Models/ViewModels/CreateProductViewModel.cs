using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 创建商品视图模型
    /// </summary>
    public class CreateProductViewModel
    {
        [Required(ErrorMessage = "商品标题不能为空")]
        [StringLength(200, ErrorMessage = "标题长度不能超过200个字符")]
        [Display(Name = "商品标题")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "商品描述不能为空")]
        [Display(Name = "商品描述")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "价格不能为空")]
        [Range(0.01, 999999.99, ErrorMessage = "价格必须在0.01到999999.99之间")]
        [Display(Name = "价格")]
        public decimal Price { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "原价必须在0.01到999999.99之间")]
        [Display(Name = "原价")]
        public decimal? OriginalPrice { get; set; }

        [Required(ErrorMessage = "请选择商品分类")]
        [Display(Name = "商品分类")]
        public int CategoryId { get; set; }

        [StringLength(100, ErrorMessage = "自提地点长度不能超过100个字符")]
        [Display(Name = "自提地点")]
        public string? PickupLocation { get; set; }

        [Display(Name = "是否可议价")]
        public bool IsNegotiable { get; set; } = false;

        [Display(Name = "商品标签")]
        public string? Tags { get; set; }

        [Display(Name = "保存为草稿")]
        public bool SaveAsDraft { get; set; } = false;
    }
}

