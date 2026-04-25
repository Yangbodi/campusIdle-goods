using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusIdleGoods.Models;
using CampusIdleGoods.Data;
using CampusIdleGoods.Helpers;
using CampusIdleGoods.Attributes;
using Microsoft.AspNetCore.SignalR;
using CampusIdleGoods.Hubs;

namespace CampusIdleGoods.Controllers
{
    /// <summary>
    /// 消息控制器
    /// </summary>
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessageController> _logger;
        private readonly IHubContext<MessageHub> _hubContext;

        public MessageController(
            ApplicationDbContext context,
            ILogger<MessageController> logger,
            IHubContext<MessageHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// 聊天列表（显示所有对话）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // 获取所有与当前用户有消息往来的用户
            var chatPartners = await _context.Messages
                .Where(m => m.SenderId == userId.Value || m.ReceiverId == userId.Value)
                .Select(m => m.SenderId == userId.Value ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var chatList = new List<dynamic>();

            foreach (var partnerId in chatPartners)
            {
                // 获取最后一条消息
                var lastMessage = await _context.Messages
                    .Where(m => (m.SenderId == userId.Value && m.ReceiverId == partnerId) ||
                               (m.SenderId == partnerId && m.ReceiverId == userId.Value))
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Include(m => m.Product)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastMessage != null)
                {
                    // 获取未读消息数量
                    var unreadCount = await _context.Messages
                        .CountAsync(m => m.SenderId == partnerId && 
                                        m.ReceiverId == userId.Value && 
                                        !m.IsRead);

                    // 获取关联的商品（如果有）
                    var product = lastMessage.Product;

                    var otherUser = lastMessage.SenderId == userId.Value 
                        ? lastMessage.Receiver 
                        : lastMessage.Sender;

                    chatList.Add(new
                    {
                        OtherUser = otherUser,
                        LastMessage = lastMessage,
                        UnreadCount = unreadCount,
                        Product = product
                    });
                }
            }

            // 按最后消息时间排序
            chatList = chatList.OrderByDescending(c => ((Message)c.LastMessage).CreatedAt).ToList();

            ViewBag.ChatList = chatList;
            ViewBag.UnreadCount = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId.Value && !m.IsRead);

            return View();
        }

        /// <summary>
        /// 发送消息（AJAX，从聊天页面调用）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content, int? productId = null)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            if (userId.Value == receiverId)
            {
                return Json(new { success = false, message = "不能给自己发送消息" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "消息内容不能为空" });
            }

            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null)
            {
                return Json(new { success = false, message = "接收者不存在" });
            }

            // 如果关联了商品，验证商品是否存在
            if (productId.HasValue)
            {
                var product = await _context.Products.FindAsync(productId.Value);
                if (product == null)
                {
                    return Json(new { success = false, message = "关联的商品不存在" });
                }
            }

            try
            {
                var message = new Message
                {
                    SenderId = userId.Value,
                    ReceiverId = receiverId,
                    ProductId = productId,
                    Content = content.Trim(),
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // 通过SignalR实时通知接收者
                await _hubContext.Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", new
                {
                    messageId = message.Id,
                    senderId = userId.Value,
                    receiverId = receiverId,
                    content = message.Content,
                    productId = productId,
                    timestamp = message.CreatedAt,
                    isRead = false
                });

                _logger.LogInformation($"用户 {userId.Value} 向用户 {receiverId} 发送消息（ID: {message.Id}）");

                return Json(new { success = true, messageId = message.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息时发生错误");
                return Json(new { success = false, message = "发送失败，请稍后重试" });
            }
        }

        /// <summary>
        /// 标记消息为已读
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                return Json(new { success = false, message = "消息不存在" });
            }

            if (message.ReceiverId != userId.Value)
            {
                return Json(new { success = false, message = "无权操作" });
            }

            try
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "已标记为已读" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记消息已读时发生错误");
                return Json(new { success = false, message = "操作失败" });
            }
        }


        /// <summary>
        /// 获取未读消息数量（AJAX）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue)
            {
                return Json(new { count = 0 });
            }

            var count = await _context.Messages
                .CountAsync(m => m.ReceiverId == userId.Value && !m.IsRead);

            return Json(new { count });
        }

        /// <summary>
        /// 聊天页面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Chat(int userId, int? productId = null)
        {
            var currentUserId = SessionHelper.GetUserId(HttpContext.Session);
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUserId.Value == userId)
            {
                TempData["ErrorMessage"] = "不能与自己聊天";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (productId.HasValue)
            {
                var product = await _context.Products
                    .Include(p => p.Images.OrderBy(i => i.SortOrder))
                    .FirstOrDefaultAsync(p => p.Id == productId.Value);
                if (product != null)
                {
                    ViewBag.ProductId = productId.Value;
                    ViewBag.Product = product;
                }
            }

            return View(user);
        }

        /// <summary>
        /// 获取聊天历史记录（AJAX）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(int userId)
        {
            var currentUserId = SessionHelper.GetUserId(HttpContext.Session);
            if (!currentUserId.HasValue)
            {
                return Json(new { success = false, message = "请先登录" });
            }

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId.Value && m.ReceiverId == userId) ||
                           (m.SenderId == userId && m.ReceiverId == currentUserId.Value))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.CreatedAt)
                .Take(50) // 最近50条消息
                .Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    createdAt = m.CreatedAt,
                    isRead = m.IsRead
                })
                .ToListAsync();

            // 标记为已读
            var unreadMessages = await _context.Messages
                .Where(m => m.ReceiverId == currentUserId.Value && 
                           m.SenderId == userId && 
                           !m.IsRead)
                .ToListAsync();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, messages });
        }
    }
}

