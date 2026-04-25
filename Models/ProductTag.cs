using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models
{
    /// <summary>
    /// 商品标签模型
    /// </summary>
    public class ProductTag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "商品ID")]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "标签名称")]
        public string TagName { get; set; } = string.Empty;

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public virtual Product Product { get; set; } = null!;
    }
}

