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
- 🔀 **三种模式** — 叠图对比、多图并排、一键合并/分图
- 🔍 **智能搜索** — 字段名关键字快速过滤定位
- 🎯 **数据溯源** — 图例自动标注数据来源（数据1/数据2）
- ⚡ **大数据优化** — 自动降采样，130MB+ CSV 流畅渲染
- 🖱️ **交互友好** — 滚轮缩放、右键平移、双击删除曲线

## 📸 截图

> 待添加截图

## 🚀 快速开始

### 下载

前往 [Releases](../../releases) 页面下载最新版本。

### 使用

1. 双击运行 `CurveAnalysis_V16.exe`
2. 点击 **添加文件** 加载 CSV 文件
3. 在左侧字段列表 **双击** 字段名 → 曲线自动绘制

就这么简单！

## 🎮 三种模式

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| **叠图模式** | 所有曲线叠加在一张图 | 对比不同参数的变化趋势 |
| **多图模式** | 多个独立图表并排显示 | 同时查看不同类型的数据 |
| **混合模式** | 一键合并/一键分图 | 灵活切换查看方式 |

## 📋 工具栏

```
添加文件 | + 新建图表  删除当前图 | 全部叠到一图  按文件分图 | 清除当前图  清除所有图  重置视图
```

## 🔧 技术栈

| 组件 | 技术 |
|------|------|
| 语言 | C# |
| 框架 | .NET 10 (WinForms) |
| 图表库 | [OxyPlot](https://github.com/oxyplot/oxyplot) 2.2.0 |
| 编码 | GBK/UTF-8 自动检测 |
| 发布 | 自包含单文件 EXE (~50MB) |

## 📁 项目结构

```
curve_analysis/
├── CurveAnalysis_CSharp/
│   ├── Program.cs              # 主程序（单文件，所有代码）
│   └── CurveAnalysis_CSharp.csproj
├── backups/                    # 历史版本源码备份
│   ├── Program_V13_Final.cs
│   └── Program_V16_Fixed.cs
├── CurveAnalysis_V13.exe       # V13 叠图版
├── CurveAnalysis_V16.exe       # V16 合并版（推荐）
├── VERSIONS.md                 # 版本架构说明
├── CHANGELOG.md                # 更新日志
└── README.md
```

## 📖 版本历史

### V16 合并版（推荐）
- V13 全部功能 + 多图表管理
- 基线对比、高对比度配色、搜索过滤
- 保存图片、文件拖拽、浮动窗口
- 网格布局、预设收藏、单击显示数据

### V13 叠图版
- 基础叠图功能
- GBK/UTF-8 编码支持
- 表头配置对话框

## 🛠️ 编译

需要 .NET 10 SDK：

```bash
cd CurveAnalysis_CSharp
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o bin/publish_v15
```

## 📝 CSV 文件格式要求

- 第一行（或指定行）为表头（字段名）
- 数据从下一行开始
- 支持 GBK 和 UTF-8 编码
- 字段用逗号分隔
- 默认使用"时戳"字段作为 X 轴

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License

---

> 💡 **为什么做这个工具？**
>
> 飞控日志分析是嵌入式工程师的日常。市面上的工具要么太重（MATLAB），要么不方便（Python 环境搭建麻烦）。这个工具的目标就是**轻量、直接、打开就用**——一个 EXE 文件，拖进来就能看数据。
