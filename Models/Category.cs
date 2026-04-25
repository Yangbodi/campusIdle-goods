using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models
{
    /// <summary>
    /// 商品分类模型
    /// </summary>
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "分类名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "分类描述")]
        public string? Description { get; set; }

        [Display(Name = "父分类ID")]
        public int? ParentId { get; set; }

        [Display(Name = "排序顺序")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "是否启用")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

