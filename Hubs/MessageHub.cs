using Microsoft.AspNetCore.SignalR;
using CampusIdleGoods.Helpers;

namespace CampusIdleGoods.Hubs
{
    /// <summary>
    /// 消息Hub - 处理实时消息通信
    /// </summary>
    public class MessageHub : Hub
    {
        private readonly ILogger<MessageHub> _logger;

        public MessageHub(ILogger<MessageHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 用户连接时
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                // 将用户添加到对应的组（使用用户ID作为组名）
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
                _logger.LogInformation($"用户 {userId.Value} 连接到消息Hub，连接ID: {Context.ConnectionId}");
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 用户断开连接时
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
                _logger.LogInformation($"用户 {userId.Value} 断开消息Hub连接");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 发送消息给指定用户
        /// </summary>
        public async Task SendMessage(int receiverId, string content, int? productId = null)
        {
            var senderId = GetUserId();
            if (!senderId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "请先登录");
                return;
            }

            if (senderId.Value == receiverId)
            {
                await Clients.Caller.SendAsync("Error", "不能给自己发送消息");
                return;
            }

            // 通知接收者（通过组）
            await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", new
            {
                senderId = senderId.Value,
                receiverId = receiverId,
                content = content,
                productId = productId,
                timestamp = DateTime.Now
            });

            // 通知发送者消息已发送
            await Clients.Caller.SendAsync("MessageSent", new
            {
                receiverId = receiverId,
                content = content,
                timestamp = DateTime.Now
            });

            _logger.LogInformation($"用户 {senderId.Value} 通过SignalR向用户 {receiverId} 发送消息");
        }

        /// <summary>
        /// 加入聊天室（与特定用户的对话）
        /// </summary>
        public async Task JoinChat(int otherUserId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return;
            }

            // 创建聊天室名称（确保顺序一致）
            var chatRoom = GetChatRoomName(userId.Value, otherUserId);
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoom);
            
            _logger.LogInformation($"用户 {userId.Value} 加入聊天室: {chatRoom}");
        }

        /// <summary>
        /// 离开聊天室
        /// </summary>
        public async Task LeaveChat(int otherUserId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return;
            }

            var chatRoom = GetChatRoomName(userId.Value, otherUserId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoom);
            
            _logger.LogInformation($"用户 {userId.Value} 离开聊天室: {chatRoom}");
        }

        /// <summary>
        /// 在聊天室中发送消息
        /// </summary>
        public async Task SendChatMessage(int otherUserId, string content, int? productId = null)
        {
            var senderId = GetUserId();
            if (!senderId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "请先登录");
                return;
            }

            var chatRoom = GetChatRoomName(senderId.Value, otherUserId);
            
            // 发送给聊天室中的所有用户
            await Clients.Group(chatRoom).SendAsync("ReceiveChatMessage", new
            {
                senderId = senderId.Value,
                receiverId = otherUserId,
                content = content,
                productId = productId,
                timestamp = DateTime.Now
            });

            _logger.LogInformation($"用户 {senderId.Value} 在聊天室 {chatRoom} 中发送消息");
        }

        /// <summary>
        /// 标记消息为已读
        /// </summary>
        public async Task MarkMessageAsRead(int messageId, int senderId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                return;
            }

            // 通知发送者消息已被阅读
            await Clients.Group($"user_{senderId}").SendAsync("MessageRead", new
            {
                messageId = messageId,
                readerId = userId.Value,
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 正在输入提示
        /// </summary>
        public async Task Typing(int receiverId, bool isTyping)
        {
            var senderId = GetUserId();
            if (!senderId.HasValue)
            {
                return;
            }

            await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new
            {
                userId = senderId.Value,
                isTyping = isTyping
            });
        }

        /// <summary>
        /// 获取当前用户ID（从Context中）
        /// </summary>
        private int? GetUserId()
        {
            // 从HttpContext的Session中获取用户ID
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                return Helpers.SessionHelper.GetUserId(httpContext.Session);
            }

            return null;
        }

        /// <summary>
        /// 获取聊天室名称（确保两个用户之间的聊天室名称一致）
        /// </summary>
        private string GetChatRoomName(int userId1, int userId2)
        {
            // 确保较小的ID在前，这样两个用户会加入同一个聊天室
            var minId = Math.Min(userId1, userId2);
            var maxId = Math.Max(userId1, userId2);
            return $"chat_{minId}_{maxId}";
        }
    }
}

