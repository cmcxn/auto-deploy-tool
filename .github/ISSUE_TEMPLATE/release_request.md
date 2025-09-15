---
name: 发布请求
about: 请求创建新版本发布
title: '🚀 Release v[版本号]'
labels: ['release', 'enhancement']
assignees: ''

---

## 发布信息

**版本号**: (例如: v2.1.0)

**发布类型**:
- [ ] 主要版本 (Major) - 包含不兼容的 API 更改
- [ ] 次要版本 (Minor) - 向下兼容的新功能
- [ ] 修补版本 (Patch) - 向下兼容的错误修复

## 更新内容

### 新功能
- [ ] 功能1
- [ ] 功能2

### 改进
- [ ] 改进1
- [ ] 改进2

### 错误修复
- [ ] 修复1
- [ ] 修复2

### 破坏性更改 (仅限主要版本)
- [ ] 更改1
- [ ] 更改2

## 发布检查清单

- [ ] 代码已合并到主分支
- [ ] 所有测试通过
- [ ] 文档已更新
- [ ] 版本号已确定
- [ ] 发布说明已准备

## 其他说明

[其他需要说明的内容]

---

**发布流程提醒**:
1. 创建 Git 标签: `git tag v[版本号] && git push origin v[版本号]`
2. 或使用 GitHub Releases 界面创建发布
3. GitHub Actions 将自动构建并发布

详细说明请参考 [流水线使用指南](https://github.com/cmcxn/auto-deploy-tool/blob/main/docs/PIPELINE_GUIDE.md)。