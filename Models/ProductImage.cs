using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusIdleGoods.Models
{
    /// <summary>
    /// 商品图片模型
    /// </summary>
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "商品ID")]
        public int ProductId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "图片路径")]
        public string ImagePath { get; set; } = string.Empty;

        [Display(Name = "排序顺序")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public virtual Product Product { get; set; } = null!;
    }
}

