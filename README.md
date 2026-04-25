# 校园闲置好物阁

该项目借助ai进行协助开发的基于 .NET 8 MVC 的校园二手商品交易平台，该项目的功能还是很丰富的，非常适合入门的小白拿来学习。

## 演示视频

https://img.wybody.top/videos/MP4/CampusldleGoods.mp4

<video width="720" controls>
  <source src="https://img.wybody.top/videos/MP4/CampusldleGoods.mp4" type="video/mp4">
  <p>您的浏览器不支持视频播放，请<a href="https://img.wybody.top/videos/MP4/CampusldleGoods.mp4">点此下载视频</a>观看。</p>
</video>

## 技术栈

- **后端框架**: .NET 8 MVC
- **消息通知**:siganalr
- **ORM**: Entity Framework Core 8.0
- **数据库**: MySQL (Pomelo.EntityFrameworkCore.MySql)
- **前端框架**: Bootstrap 5
- **验证**: jQuery Validation + jQuery Validation Unobtrusive

## 数据库配置

- **主机**: localhost
- **端口**: 3306
- **数据库名**: CampusIdleGoods
- **用户名**: admin
- **密码**: 123456

连接字符串已配置在 `appsettings.json` 中。

## 项目结构

### 数据模型 (Models/)

- **User.cs** - 用户模型（包含认证信息、实名信息、联系方式等）
- **Category.cs** - 商品分类模型（支持多级分类）
- **Product.cs** - 商品模型（包含状态管理、审核流程等）
- **ProductImage.cs** - 商品图片模型
- **ProductTag.cs** - 商品标签模型
- **Message.cs** - 站内消息模型
- **Favorite.cs** - 收藏模型

### 数据访问 (Data/)

- **ApplicationDbContext.cs** - EF Core 数据库上下文，包含所有实体的配置和关系

## 数据库迁移

初始迁移已创建：`InitialCreate`

### 应用迁移到数据库

运行以下命令将迁移应用到数据库：

```bash
dotnet ef database update
```

### 创建新迁移

当修改模型后，创建新迁移：

```bash
dotnet ef migrations add MigrationName
```

### 管理后台

当前只能自己注册以后，在数据库的IsAdmin设置为1，登录后就可以对商品进行审批，以及用户管理。要是需要可以自行进行扩展。

## 功能

### 一 用户认证模块

- [x] 配置身份认证和Session管理
- [x] 创建认证服务（密码哈希、邮箱验证等）
- [x] 用户注册功能（包含学号格式验证）
- [x] 用户登录/登出功能
- [x] 密码找回功能（通过邮箱发送重置密码的连接）
- [x] 个人中心（查看、编辑、修改密码）
- [x] 头像上传功能
- [x] 授权和权限控制

### 二 商品管理模块

- [x] 创建商品发布相关的ViewModel
- [x] 创建ProductController（发布、编辑、删除商品）
- [x] 实现商品图片上传功能（支持拖拽，最多5张）
- [x] 创建商品发布视图
- [x] 创建商品编辑视图
- [x] 创建商品详情页面
- [x] 实现分类选择器（多级分类）
- [x] 实现标签系统和草稿保存功能
- [x] 商品状态管理（草稿、待审核、已上架、已下架、已售出）
- [x] 我的商品列表页面

### 三：商品浏览与发现模块

- [x] 更新首页显示最新商品（瀑布流展示）
- [x] 实现热门分类展示（从数据库动态加载）
- [x] 创建商品浏览页面（Browse）
- [x] 实现关键词搜索功能（标题、描述、标签）
- [x] 实现高级筛选功能（分类、价格区间、发布时间）
- [x] 实现排序功能（最新、价格、浏览量）
- [x] 实现分页功能
- [x] 创建分类浏览页面（Category）
- [x] 商品卡片悬停效果优化

### 四：商品审核模块

- [x] 创建AdminController（审核管理）
- [x] 实现管理员仪表板（数据统计）
- [x] 实现待审核商品列表
- [x] 实现审核通过/拒绝功能
- [x] 创建审核商品详情页面
- [x] 实现批量审核功能
- [x] 实现审核历史记录
- [x] 审核标准检查点（信息完整性验证）

### 五 交互与沟通模块

- [x] 创建MessageController（站内消息管理）
- [x] 实现发送消息功能（支持关联商品）
- [x] 实现收件箱/发件箱列表（分页显示）
- [x] 实现消息详情和标记已读功能
- [x] 在商品详情页添加联系卖家功能
- [x] 创建FavoriteController（收藏管理）
- [x] 实现收藏/取消收藏功能（AJAX）
- [x] 实现我的收藏列表页面
- [x] 在导航栏添加消息和收藏入口
- [x] 未读消息数量提示
- [x] **集成SignalR实现实时聊天功能**
  - [x] 创建MessageHub（SignalR Hub）
  - [x] 实现实时消息发送和接收
  - [x] 实现聊天室功能
  - [x] 实现"正在输入"提示
  - [x] 实现消息已读通知
  - [x] 创建实时聊天界面
  - [x] 自动加载历史消息

### 六：管理员后台模块（已完成）

- [x] 用户管理功能
  - [x] 用户列表（搜索、筛选、分页）
  - [x] 用户详情查看
  - [x] 编辑用户信息
  - [x] 启用/禁用用户
  - [x] 设置/取消管理员权限
  - [x] 用户数据统计
- [x] 分类管理功能
  - [x] 分类列表（树形结构展示）
  - [x] 添加分类（支持设置父分类）
  - [x] 编辑分类
  - [x] 删除分类（检查子分类和商品）
  - [x] 启用/禁用分类
  - [x] 分类排序管理

## 运行项目

1. 确保 MySQL 服务已启动
2. 确保数据库已创建（或运行 `dotnet ef database update` 自动创建）
3. 运行项目：

```bash
dotnet run
```

## 注意事项

- 首次运行前需要执行数据库迁移
- 确保 MySQL 用户有创建数据库的权限
- 开发环境使用 `appsettings.Development.json` 配置
