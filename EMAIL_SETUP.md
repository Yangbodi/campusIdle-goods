# 邮箱验证功能配置说明

## 功能概述

邮箱验证功能已集成到系统中,用户必须验证邮箱后才能发布商品。系统提供以下功能:

1. **注册时自动发送验证邮件** - 用户注册后会收到验证邮件
2. **邮箱验证** - 用户点击邮件中的链接完成验证
3. **重新发送验证邮件** - 在个人中心可以重新发送验证邮件
4. **密码重置** - 通过邮箱接收密码重置链接
5. **发布商品限制** - 只有验证邮箱的用户才能发布商品

## 邮箱配置

### 1. 配置文件位置

邮箱配置在 `appsettings.json` 文件中:

```json
{
  "Email": {
    "SenderEmail": "yangbodi923@gmail.com",
    "SenderPassword": "您的邮箱密码或应用专用密码",
    "SenderName": "校园闲置好物阁",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587"
  },
  "AppSettings": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### 2. Gmail 配置步骤

如果您使用 Gmail,需要按照以下步骤配置:

#### 步骤1: 启用两步验证
1. 登录您的 Google 账户
2. 访问 https://myaccount.google.com/security
3. 在"登录Google"部分,启用"两步验证"

#### 步骤2: 生成应用专用密码
1. 访问 https://myaccount.google.com/apppasswords
2. 选择应用类型为"邮件"
3. 选择设备类型为"Windows 电脑"或"其他"
4. 点击"生成"
5. 复制生成的16位密码(格式如: `xxxx xxxx xxxx xxxx`)
6. 将此密码配置到 `appsettings.json` 的 `SenderPassword` 字段

#### 步骤3: 更新配置
```json
{
  "Email": {
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "xxxx xxxx xxxx xxxx",  // 应用专用密码
    "SenderName": "校园闲置好物阁",
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587"
  }
}
```

### 3. 其他邮箱服务商配置

#### QQ邮箱
```json
{
  "Email": {
    "SenderEmail": "your-email@qq.com",
    "SenderPassword": "授权码",  // 不是QQ密码,是在QQ邮箱设置中生成的授权码
    "SenderName": "校园闲置好物阁",
    "SmtpHost": "smtp.qq.com",
    "SmtpPort": "587"
  }
}
```

获取QQ邮箱授权码:
1. 登录 QQ 邮箱
2. 设置 -> 账户 -> POP3/IMAP/SMTP/Exchange/CardDAV/CalDAV服务
3. 开启 IMAP/SMTP 服务
4. 生成授权码

#### 163邮箱
```json
{
  "Email": {
    "SenderEmail": "your-email@163.com",
    "SenderPassword": "授权码",
    "SenderName": "校园闲置好物阁",
    "SmtpHost": "smtp.163.com",
    "SmtpPort": "465"  // 注意163使用465端口
  }
}
```

### 4. 配置应用基础URL

根据您的部署环境配置正确的 `BaseUrl`:

- **本地开发**: `http://localhost:5000`
- **生产环境**: `https://yourdomain.com`

```json
{
  "AppSettings": {
    "BaseUrl": "http://localhost:5000"  // 修改为实际URL
  }
}
```

## 数据库迁移

邮箱验证功能需要新的数据库字段,已创建迁移文件 `AddEmailVerificationFields`。

### 应用迁移
```bash
dotnet ef database update
```

### 新增字段
- `EmailVerificationToken` - 邮箱验证令牌
- `EmailVerificationTokenExpires` - 令牌过期时间(24小时)
- `PasswordResetToken` - 密码重置令牌
- `PasswordResetTokenExpires` - 令牌过期时间(1小时)

## 使用流程

### 用户注册流程
1. 用户填写注册信息并提交
2. 系统创建用户账户(邮箱未验证状态)
3. 系统生成验证令牌并发送验证邮件
4. 用户收到邮件,点击验证链接
5. 系统验证令牌有效性
6. 验证成功,用户可以发布商品

### 重新发送验证邮件
1. 登录系统
2. 进入个人中心
3. 如果邮箱未验证,会看到警告提示
4. 点击"重新发送验证邮件"按钮
5. 系统生成新令牌并发送邮件

### 密码重置流程
1. 在登录页点击"忘记密码"
2. 输入注册邮箱
3. 系统发送密码重置链接
4. 用户收到邮件,点击重置链接
5. 输入新密码
6. 密码重置成功

## 测试验证

### 1. 测试注册发送邮件
```bash
# 运行项目
dotnet run

# 访问注册页面
http://localhost:5000/Account/Register

# 填写信息并注册,查看是否收到验证邮件
```

### 2. 测试验证链接
- 点击邮件中的验证链接
- 应该跳转到登录页并显示"邮箱验证成功"

### 3. 测试发布商品限制
- 邮箱未验证时,访问 `/Product/Create`
- 应该被重定向到个人中心,并显示错误提示

### 4. 测试重新发送验证邮件
- 登录后进入个人中心
- 如果未验证,点击"重新发送验证邮件"
- 应该收到新的验证邮件

## 安全说明

1. **令牌过期时间**:
   - 邮箱验证令牌: 24小时
   - 密码重置令牌: 1小时

2. **令牌使用后自动清除**: 验证成功或密码重置成功后,相关令牌会被清除

3. **密码不应明文存储**: 请不要将邮箱密码提交到版本控制系统

4. **建议使用环境变量**: 在生产环境中,建议使用环境变量或密钥管理服务存储敏感信息

## 故障排查

### 邮件发送失败
1. 检查邮箱配置是否正确
2. 确认使用的是应用专用密码,不是邮箱登录密码
3. 检查SMTP服务器地址和端口
4. 查看应用日志获取详细错误信息

### 验证链接无效
1. 检查令牌是否过期(24小时)
2. 确认 `BaseUrl` 配置正确
3. 检查邮件中的链接是否完整

### 无法发布商品
1. 确认邮箱已验证
2. 在个人中心查看验证状态
3. 如未验证,重新发送验证邮件

## 注意事项

1. 首次使用前,请务必配置正确的邮箱信息
2. 在生产环境部署前,修改 `BaseUrl` 为实际域名
3. 建议定期清理过期的验证令牌(可以添加定时任务)
4. 邮件发送失败不会阻止用户注册,但用户需要验证邮箱才能发布商品

## 后续优化建议

1. 添加邮件发送队列,避免发送邮件时阻塞请求
2. 添加邮件发送日志,方便追踪问题
3. 支持自定义邮件模板
4. 添加邮件发送频率限制,防止滥用
5. 添加定时任务清理过期令牌

