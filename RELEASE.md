# 发布指南

## 自动发布流程

这个项目配置了 GitHub Actions 来自动构建和发布 AutoDeployTool。

### 创建新版本发布

1. **创建新的版本标签**：
   ```bash
   git tag v2.1.0
   git push origin v2.1.0
   ```

2. **或者通过 GitHub 网页界面**：
   - 转到 GitHub 仓库页面
   - 点击 "Releases" 标签
   - 点击 "Create a new release"
   - 输入标签版本（如 `v2.1.0`）
   - 添加发布说明
   - 点击 "Publish release"

### 手动触发构建

你也可以通过 GitHub Actions 页面手动触发构建：

1. 转到 Actions 标签
2. 选择 "Build and Release" 工作流
3. 点击 "Run workflow"
4. 输入要发布的标签版本

### 构建产物

每次发布都会自动生成：
- `AutoDeployTool-{version}.zip` - 包含完整的可执行文件和依赖项
- 自动创建的 GitHub Release 页面
- 中文发布说明和下载指南

### 版本命名规范

建议使用语义化版本控制：
- `v1.0.0` - 主要版本更新
- `v1.1.0` - 功能更新
- `v1.0.1` - 错误修复

### 系统要求

构建需要：
- Windows 运行器（Windows Server 2022）
- .NET 8.0 SDK
- GitHub Actions 权限

发布的应用程序需要：
- Windows 10 或更高版本
- .NET 8.0 Runtime（如果未包含在发布包中）