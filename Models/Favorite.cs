using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models
{
    /// <summary>
    /// 收藏模型
    /// </summary>
    public class Favorite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "用户ID")]
        public int UserId { get; set; }

        [Required]
        [Display(Name = "商品ID")]
        public int ProductId { get; set; }

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}

