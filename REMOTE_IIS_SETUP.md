# 远程IIS管理配置指南

## 概述
此自动部署工具现在支持远程IIS管理功能，可以停止和启动远程服务器上的IIS站点。此功能使用PowerShell远程管理（PSRemoting）实现。

## 前提条件

### 目标服务器配置
1. **启用WinRM服务**
   ```powershell
   # 在目标服务器上以管理员身份运行
   Enable-PSRemoting -Force
   winrm quickconfig
   ```

2. **配置防火墙**
   ```powershell
   # 允许WinRM HTTP流量（端口5985）
   New-NetFirewallRule -DisplayName "WinRM-HTTP" -Direction Inbound -Protocol TCP -LocalPort 5985 -Action Allow
   ```

3. **设置执行策略**
   ```powershell
   Set-ExecutionPolicy RemoteSigned -Force
   ```

4. **确保IIS管理模块可用**
   ```powershell
   # 导入WebAdministration模块
   Import-Module WebAdministration
   ```

### 网络要求
- 客户端能够通过TCP 5985端口连接到目标服务器
- 网络防火墙允许WinRM流量
- DNS解析正常或使用IP地址

## 使用方法

### 配置步骤
1. 在"服务器配置"标签页中：
   - **服务器地址**: 输入目标服务器的IP地址或主机名
   - **用户名**: 输入有权限管理IIS的域用户或本地用户
   - **密码**: 输入对应的密码
   - **IIS站点名**: 输入要管理的IIS站点名称

2. 启用远程IIS管理：
   - 勾选"使用远程IIS管理"复选框

3. 测试连接：
   - 点击"测试远程连接"按钮验证配置

### 支持的操作
- 停止远程IIS站点
- 启动远程IIS站点
- 验证远程IIS站点状态

## 故障排除

### 常见错误及解决方案

1. **连接被拒绝**
   - 检查WinRM服务是否正在运行
   - 验证防火墙配置
   - 确认网络连接正常

2. **认证失败**
   - 验证用户名和密码
   - 确保用户有远程登录权限
   - 检查用户账户状态

3. **找不到IIS站点**
   - 确认站点名称拼写正确
   - 验证站点是否存在于目标服务器
   - 检查用户是否有IIS管理权限

4. **PowerShell执行错误**
   - 确认目标服务器支持PowerShell远程管理
   - 验证WebAdministration模块是否可用
   - 检查执行策略设置

### 诊断命令
```powershell
# 在目标服务器上测试WinRM配置
winrm enumerate winrm/config/listener

# 检查IIS站点
Get-Website

# 测试PowerShell远程连接
Test-WSMan -ComputerName <目标服务器>
```

## 安全考虑

1. **使用安全凭据**
   - 避免使用明文密码
   - 考虑使用专用的服务账户
   - 定期更换密码

2. **网络安全**
   - 在生产环境中考虑使用HTTPS (端口5986)
   - 限制WinRM访问的IP范围
   - 使用VPN或专用网络

3. **权限最小化**
   - 为部署账户分配最小必要权限
   - 避免使用域管理员账户
   - 考虑使用组管理的服务账户 (gMSA)

## 兼容性

- **操作系统**: Windows Server 2012 R2 及更高版本
- **PowerShell**: 版本 3.0 及更高版本
- **IIS**: 版本 8.5 及更高版本
- **.NET Framework**: 4.7.2 及更高版本

## 性能优化

- 连接超时: 30秒
- 操作超时: 2分钟
- 建议在部署前测试连接
- 考虑网络延迟对部署时间的影响