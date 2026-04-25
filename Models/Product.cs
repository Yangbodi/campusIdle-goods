using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusIdleGoods.Models
{
    /// <summary>
    /// 商品状态枚举
    /// </summary>
    public enum ProductStatus
    {
        Draft = 0,           // 草稿
        PendingReview = 1,   // 待审核
        Published = 2,       // 已上架
        Sold = 3,           // 已售出
        Removed = 4,        // 已下架
        Rejected = 5        // 审核拒绝
    }

    /// <summary>
    /// 商品模型
    /// </summary>
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "商品标题")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "text")]
        [Display(Name = "商品描述")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "价格")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "原价")]
        public decimal? OriginalPrice { get; set; }

        [Required]
        [Display(Name = "分类ID")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "卖家ID")]
        public int SellerId { get; set; }

        [StringLength(100)]
        [Display(Name = "自提地点")]
        public string? PickupLocation { get; set; }

        [Display(Name = "是否可议价")]
        public bool IsNegotiable { get; set; } = false;

        [Display(Name = "商品状态")]
        public ProductStatus Status { get; set; } = ProductStatus.Draft;

        [Display(Name = "浏览次数")]
        public int ViewCount { get; set; } = 0;

        [StringLength(500)]
        [Display(Name = "拒绝原因")]
        public string? RejectReason { get; set; }

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "更新时间")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Display(Name = "审核时间")]
        public DateTime? ReviewedAt { get; set; }

        [Display(Name = "审核人ID")]
        public int? ReviewerId { get; set; }

        // 导航属性
        public virtual Category Category { get; set; } = null!;
        public virtual User Seller { get; set; } = null!;
        public virtual User? Reviewer { get; set; }
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<ProductTag> Tags { get; set; } = new List<ProductTag>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    }
}

