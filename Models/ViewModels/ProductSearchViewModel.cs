using System.ComponentModel.DataAnnotations;

namespace CampusIdleGoods.Models.ViewModels
{
    /// <summary>
    /// 商品搜索视图模型
    /// </summary>
    public class ProductSearchViewModel
    {
        [Display(Name = "关键词")]
        public string? Keyword { get; set; }

        [Display(Name = "分类")]
        public int? CategoryId { get; set; }

        [Display(Name = "最低价格")]
        public decimal? MinPrice { get; set; }

        [Display(Name = "最高价格")]
        public decimal? MaxPrice { get; set; }

        [Display(Name = "发布时间")]
        public string? TimeFilter { get; set; } // 24小时内、一周内、一个月内

        [Display(Name = "排序方式")]
        public string? SortBy { get; set; } // latest, price_asc, price_desc, views

        [Display(Name = "页码")]
        public int Page { get; set; } = 1;

        [Display(Name = "每页数量")]
        public int PageSize { get; set; } = 12;

        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}

