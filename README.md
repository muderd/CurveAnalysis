# 📊 Flight Log Curve Analyzer

**飞控日志曲线分析工具** — 轻量级 CSV 数据可视化利器，专为嵌入式/飞控工程师打造。

[![.NET](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)]()
[![License](https://img.shields.io/badge/License-MIT-green)]()

---

## ✨ 特性

- 🚀 **开箱即用** — 单文件 EXE，无需安装，双击即跑
- 📂 **多文件支持** — 同时加载多个 CSV 文件，自动识别 GBK/UTF-8 编码
- 📈 **灵活绘图** — 双击字段即可绘制时序曲线，支持拖拽操作
- 🔀 **两种版本** — V13 叠图版（轻量）/ V16 合并版（全功能）
- 🔍 **智能搜索** — 字段名关键字快速过滤定位
- 🎯 **数据溯源** — 图例自动标注数据来源
- ⚡ **大数据优化** — 自动降采样，130MB+ CSV 流畅渲染
- 🖱️ **交互友好** — 滚轮缩放、右键平移、双击删除曲线

---

## 📦 版本说明

| 版本 | 文件 | 功能范围 | 适用场景 |
|------|------|----------|----------|
| **V13** | `CurveAnalysis_V13.exe` | 基础叠图 | 快速查看单文件数据 |
| **V16** | `CurveAnalysis_V16.exe` | 全功能合并版 | 多文件对比分析 |

### V13 叠图版
- 添加文件、清除图表、重置视图
- 字段搜索/双击绘图/拖拽绘图
- X轴选择、背景色切换、网格线开关
- GBK/UTF-8 编码自动检测
- 表头配置对话框

### V16 合并版（推荐）
V13 全部功能 + 以下增强：

| 功能 | 说明 |
|------|------|
| **多图表管理** | 新建/删除/清除独立图表 |
| **全部叠到一图** | 一键合并所有曲线到同一图表 |
| **按文件分图** | 按文件自动分图，仅显示已选字段 |
| **自适应布局** | 自动计算行列布局，支持手动调整 |
| **基线对比** | 红色竖线贯穿所有图表，交叉点显示数据标签 |
| **高对比度配色** | 12色 RGB 配色方案 |
| **搜索过滤** | 输入关键字快速定位字段 |
| **保存图片** | 右键图表 → 保存为 PNG |
| **文件拖拽** | 从资源管理器直接拖入 CSV |
| **X轴粒度** | 手动设置 X 轴刻度间隔 |
| **背景色切换** | 白色/浅灰/黑色/深灰 |
| **预设收藏** | 保存常用曲线组合，一键应用 |
| **浮动窗口** | 图表弹出到独立窗口 |
| **网格布局** | M行×N列自定义排列 |
| **单击显示数据** | 点击图表显示所有曲线在该位置的 Y 值 |
| **标签防重叠** | Y 值接近时自动错开标签 |

---

## 🚀 快速开始

### 下载

前往 [Releases](../../releases) 页面下载最新版本。

### 使用

1. 双击运行 `CurveAnalysis_V16.exe`
2. 点击 **添加文件** 加载 CSV 文件
3. 在左侧字段列表 **双击** 字段名 → 曲线自动绘制

就这么简单！

---

## 🎮 V16 工具栏

```
添加文件 | + 新建图表  删除当前图 | 清除当前图  清除所有图 | 全部叠到一图  按文件分图 | 布局 [行] x [列] [应用布局] [自动] | 基线对比 | 重置视图
```

### 按文件分图

点击后自动为每个已选字段的文件创建独立图表：
- 已选字段的文件 → 创建图表，只显示已选字段
- 未选字段的文件 → 跳过，不创建图表（避免加载卡死）
- 布局自动设为最接近正方形的行列数

### 布局控制

- **默认自适应**：根据图表数量自动计算最佳行列布局
- **手动调整**：输入行数和列数 → 点击"应用布局"
- **恢复自适应**：点击"自动"按钮

---

## 📋 技术栈

| 组件 | 技术 |
|------|------|
| 语言 | C# |
| 框架 | .NET 10 (WinForms) |
| 图表库 | [OxyPlot](https://github.com/oxyplot/oxyplot) 2.2.0 |
| 编码 | GBK/UTF-8 自动检测 |
| 发布 | 自包含单文件 EXE (~50MB) |

---

## 📁 项目结构

```
curve_analysis/
├── CurveAnalysis_CSharp/
│   ├── Program.cs                    # 主程序（单文件）
│   └── CurveAnalysis_CSharp.csproj
├── backups/
│   ├── Program_V13_Final.cs          # V13 源码备份
│   └── Program_V16_Fixed.cs          # V16 修复版备份
├── CurveAnalysis_V13.exe             # V13 叠图版
├── CurveAnalysis_V16.exe             # V16 合并版
├── VERSIONS.md                       # 版本架构说明
├── CHANGELOG.md                      # 更新日志
└── README.md                         # 本文档
```

---

## 📖 版本历史

详见 [CHANGELOG.md](CHANGELOG.md)

---

## 🛠️ 编译

需要 .NET 10 SDK：

```bash
cd CurveAnalysis_CSharp

# V16（当前版本）
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o bin/publish

# V13（叠图版）
cp ../backups/Program_V13_Final.cs Program.cs
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o bin/publish_v13
```

---

## 📝 CSV 文件格式要求

- 第一行（或指定行）为表头（字段名）
- 数据从下一行开始
- 支持 GBK 和 UTF-8 编码
- 字段用逗号分隔
- 默认使用"时戳"字段作为 X 轴
- 典型文件包含 400+ 列数据字段

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

## 📄 许可证

MIT License

---

> 💡 **为什么做这个工具？**
>
> 飞控日志分析是嵌入式工程师的日常。市面上的工具要么太重（MATLAB），要么不方便（Python 环境搭建麻烦）。这个工具的目标就是**轻量、直接、打开就用**——一个 EXE 文件，拖进来就能看数据。
