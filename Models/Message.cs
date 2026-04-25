using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models
{
    /// <summary>
    /// 站内消息模型
    /// </summary>
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "发送者ID")]
        public int SenderId { get; set; }

        [Required]
        [Display(Name = "接收者ID")]
        public int ReceiverId { get; set; }

        [Display(Name = "关联商品ID")]
        public int? ProductId { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "消息内容")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "是否已读")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public virtual User Sender { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
        public virtual Product? Product { get; set; }
    }
}

