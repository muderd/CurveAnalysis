# 飞控日志曲线分析工具

轻量级 CSV 数据可视化工具，专为嵌入式/飞控工程师打造。双击字段即可绘制时序曲线，一个 EXE 拖进来就能看数据。

## 技术栈

- C# / .NET 10 (WinForms)
- [OxyPlot](https://github.com/oxyplot/oxyplot) 2.2.0 图表库
- 自包含单文件 EXE（~50MB），无需安装运行时

## 两个版本

### V13 叠图版（轻量）

基础叠图功能，适合快速查看单文件数据。

**功能：**
- CSV 文件加载，GBK/UTF-8 编码自动检测
- 表头配置对话框，可配置表头行号和数据起始行
- 字段列表，支持搜索过滤
- 双击字段名或拖拽字段到图表区绘制曲线
- X 轴选择（默认"时戳"字段，可切换）
- 滚轮缩放、右键平移、双击删除曲线
- 背景色切换、网格线开关

**运行：** 双击 `CurveAnalysis_V13.exe`

### V16 合并版（推荐）

V13 全部功能 + 多图表管理 + 高级分析功能。

**新增功能：**
- 多图表管理（新建/删除/清除独立图表）
- 一键合并所有曲线到同一图表
- 按文件自动分图（仅显示已加载的字段）
- 网格布局（M×N 自定义排列）
- 基线对比（红色竖线贯穿所有图表，交叉点显示数据标签）
- 高对比度 12 色 RGB 配色
- 搜索框模糊过滤
- 图表保存为 PNG 图片
- 文件拖拽加载（从资源管理器直接拖入 CSV）
- X 轴粒度控制
- 背景色切换（白色/浅灰/黑色/深灰）
- 预设收藏（保存常用曲线组合，一键应用）
- 浮动窗口（图表弹出到独立窗口）
- 单击显示数据（点击图表显示所有曲线在该位置的 Y 值）
- 标签防重叠（Y 值接近时自动错开）

**运行：** 双击 `CurveAnalysis_V16.exe`

## 快速开始

1. 下载 `CurveAnalysis_V16.exe`（或 V13 叠图版）
2. 双击运行
3. 点击 **添加文件** 加载 CSV 文件
4. 在左侧字段列表 **双击** 字段名 → 曲线自动绘制

## 数据格式要求

- CSV 文件，GBK 或 UTF-8 编码
- 第一行（或指定行）为表头（字段名）
- 数据从下一行开始
- 字段用逗号分隔
- 默认使用"时戳"字段作为 X 轴
- 典型文件包含 400+ 列数据字段

## 编译

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

## 源码修改指南

代码结构清晰，便于自行修改：

1. **修改预设颜色**：搜索 `PRESET_COLORS` 或配色相关数组
2. **修改降采样阈值**：搜索 `DsThresh` 常量（默认 8000）
3. **修改默认 X 轴字段**：搜索 X 轴相关逻辑
4. **添加新功能**：在 `Program.cs` 中添加 UI 控件和事件处理
