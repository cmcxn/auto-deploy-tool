# 使用发布流水线指南

## 概述

本项目现已配置了自动化的 GitHub Actions CI/CD 流水线，可以自动构建和发布 AutoDeployTool 到 GitHub Releases。

## 自动发布流程

### 1. 通过 Git 标签触发发布

```bash
# 创建新版本标签
git tag v2.1.0

# 推送标签到远程仓库
git push origin v2.1.0
```

### 2. 通过 GitHub 界面创建发布

1. 访问仓库的 GitHub 页面
2. 点击 "Releases" 选项卡
3. 点击 "Create a new release"
4. 输入标签版本（例如：`v2.1.0`）
5. 填写发布标题和说明
6. 点击 "Publish release"

### 3. 手动触发工作流

1. 转到 GitHub 仓库的 "Actions" 选项卡
2. 选择 "Build and Release" 工作流
3. 点击 "Run workflow" 按钮
4. 输入要创建的标签版本
5. 点击 "Run workflow"

## 发布内容

每次发布将自动生成：

- **构建产物**: `AutoDeployTool-{version}.zip`
  - 包含编译后的可执行文件
  - 包含所有必要的依赖项
  - 可直接在 Windows 系统上运行

- **GitHub Release**:
  - 自动生成的发布页面
  - 中文发布说明
  - 下载链接和安装指南

## 持续集成

除了发布流水线，还配置了持续集成工作流：

- **触发条件**: 
  - 推送到 `main` 或 `master` 分支
  - 针对 `main` 或 `master` 分支的 Pull Request

- **执行内容**:
  - 构建验证
  - 依赖项检查
  - 基本测试运行

## 技术细节

### 构建环境
- **运行器**: Windows Server 2022 (windows-latest)
- **SDK**: .NET 8.0
- **构建配置**: Release

### 发布包结构
```
AutoDeployTool-v2.1.0.zip
├── AutoDeployTool.exe          # 主程序
├── AutoDeployTool.dll          # 应用程序库
├── *.dll                       # 依赖库文件
├── AutoDeployTool.runtimeconfig.json
└── AutoDeployTool.deps.json
```

### 版本命名规范

建议使用 [语义化版本控制](https://semver.org/lang/zh-CN/)：

- `v1.0.0` - 主版本号：不兼容的 API 修改
- `v1.1.0` - 次版本号：向下兼容的功能性新增
- `v1.0.1` - 修订号：向下兼容的问题修正

## 故障排除

### 常见问题

1. **构建失败**
   - 检查 .NET 项目文件是否有效
   - 验证依赖项是否可用
   - 查看 Actions 日志获取详细错误信息

2. **发布失败**
   - 确认 GitHub Token 权限正确
   - 检查标签名称格式（建议以 `v` 开头）
   - 验证仓库设置允许创建发布

3. **下载问题**
   - 确认发布不是草稿状态
   - 检查文件是否成功上传到 Release

### 查看日志

1. 转到 GitHub 仓库的 "Actions" 选项卡
2. 找到相应的工作流运行
3. 点击查看详细的构建和发布日志

## 本地构建

也可以使用提供的 PowerShell 脚本进行本地构建：

```powershell
# 基本构建
.\Build.ps1

# 指定版本号
.\Build.ps1 -Version "v2.1.0"

# 清理后构建
.\Build.ps1 -Version "v2.1.0" -Clean
```

---

有关更多技术细节，请参考项目的 [README.md](README.md) 和 [RELEASE.md](RELEASE.md) 文件。