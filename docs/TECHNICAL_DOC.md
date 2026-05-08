# 飞控日志曲线分析工具 - 技术文档

## 项目概述

**名称：** 飞控日志曲线分析工具  
**用途：** 加载多个飞控日志 CSV 文件，选择字段绘制时序曲线，支持叠加对比分析  
**技术栈：** C# WinForms + OxyPlot 2.2.0 + .NET 10  
**输出：** 自包含单文件 exe（~50MB），无需安装运行时

## 功能清单

### 核心功能
1. **CSV 文件加载** - 支持 GBK/UTF-8 编码自动检测，懒加载表头和列数据
2. **表头配置** - 可配置表头行号和数据起始行，带预览
3. **字段列表** - 按文件分组显示，支持搜索过滤（模糊匹配）
4. **曲线绘制** - 双击或拖拽字段到图表区，每条曲线独立颜色
5. **X 轴选择** - 默认"时戳"字段，可切换任意字段作为 X 轴
6. **图表交互** - 滚轮缩放、右键平移、双击删除曲线、单击显示数据
7. **图例显示** - 底部水平排列，显示字段名，自动标注数据来源
8. **文件管理** - 支持添加/替换/删除文件，支持文件拖拽加载

### V16 新增功能
- **多图表管理** - 新建/删除/清除独立图表
- **全部叠到一图** - 一键合并所有曲线到第 1 个图表
- **按文件分图** - 自动为每个文件创建独立图表，仅显示已加载字段
- **网格布局** - M×N 自定义排列，支持自适应布局
- **基线对比** - 红色竖线贯穿所有图表，交叉点显示数据标签
- **标签防重叠** - Y 值接近时自动错开标签
- **高对比度配色** - 12 色 RGB 配色方案
- **曲线加粗** - StrokeThickness 从 1.5 提升到 2.5
- **搜索过滤** - 输入关键字快速定位字段
- **保存图片** - 右键图表保存为 PNG
- **文件拖拽** - 从资源管理器直接拖入 CSV
- **X 轴粒度控制** - 手动设置 X 轴刻度间隔
- **背景色切换** - 白色/浅灰/黑色/深灰四种主题
- **预设收藏** - 保存常用曲线组合，一键应用
- **浮动窗口** - 图表弹出到独立窗口
- **单击显示数据** - 点击图表显示所有曲线在该位置的 Y 值

## 关键技术实现

### 1. GBK 编码检测

```csharp
// .NET 8+ 需要在 Main() 中注册编码提供程序
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// 读取前 8KB，检查 BOM，然后尝试 GBK 解码
byte[] head = File.ReadAllBytes(path).Take(8192).ToArray();
var gbk = Encoding.GetEncoding("gb18030");
var text = gbk.GetString(head);
if (text.Split('\n')[0].Any(c => c >= 0x4e00 && c <= 0x9fff))
    return "gb18030";
```

### 2. 懒加载列数据

```csharp
// 首次访问时从磁盘读取，后续从缓存返回
public double[] GetData(string col)
{
    if (Cache.TryGetValue(col, out var c)) return c;
    var d = CsvReader.ReadColumn(FilePath, col, FileEncoding, HeaderRow, DataStartRow);
    Cache[col] = d;
    return d;
}
```

### 3. 降采样

```csharp
// 当数据点超过 8000 个时，按步长抽样以保持渲染性能
if (xs.Length > DsThresh)
{
    int step = xs.Length / DsThresh;
    xs = xs.Where((_, i) => i % step == 0).ToArray();
    ys = ys.Where((_, i) => i % step == 0).ToArray();
}
```

### 4. 双击删除曲线

```csharp
private void PlotView_MouseDown(object? s, MouseEventArgs e)
{
    if (e.Clicks == 2 && _plotted.Count > 0)
    {
        var last = _plotted[^1];
        _plotModel.Series.Remove(last.series);
        _plotted.Remove(last);
        _plotModel.InvalidatePlot(true);
    }
}
```

### 5. OxyPlot 图表配置

```csharp
// 图例放在底部，水平排列
_plotModel.Legends.Add(new Legend {
    LegendPosition = LegendPosition.BottomCenter,
    LegendPlacement = LegendPlacement.Outside,
    LegendOrientation = LegendOrientation.Horizontal
});

// 基线竖线
_plotModel.Annotations.Add(new LineAnnotation {
    Type = LineAnnotationType.Vertical,
    X = baselineValue,
    Color = OxyColors.Red,
    StrokeThickness = 2
});
```

### 6. 标签防重叠

```csharp
// 按 Y 值排序，间距 < 4% Y 范围时自动错开
var sorted = labels.OrderBy(l => l.Y).ToList();
for (int i = 1; i < sorted.Count; i++)
{
    double gap = sorted[i].Y - sorted[i-1].Y;
    if (gap < yRange * 0.04)
        sorted[i].Y = sorted[i-1].Y + yRange * 0.04;
}
```

## 文件结构

```
curve_analysis/
├── CurveAnalysis_CSharp/
│   ├── Program.cs                    # 主程序（单文件，所有代码）
│   ├── CurveAnalysis_CSharp.csproj
│   └── REQUIREMENTS.md
├── backups/
│   ├── Program_V13_Final.cs          # V13 源码备份
│   └── Program_V16_Fixed.cs          # V16 修复版备份
├── CurveAnalysis_V13.exe             # V13 叠图版
├── CurveAnalysis_V16.exe             # V16 合并版（推荐）
├── VERSIONS.md                       # 版本架构说明
├── CHANGELOG.md                      # 更新日志
└── README.md                         # 使用说明
```

## 编译和发布

```bash
# 需要 .NET 10 SDK
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

## 依赖包

| 包名 | 版本 | 用途 |
|------|------|------|
| OxyPlot.Core | 2.2.0 | 图表核心 |
| OxyPlot.WindowsForms | 2.2.0 | WinForms 集成 |

## 已知限制

1. **仅支持 CSV** - Excel（.xlsx）不支持
2. **仅限 Windows** - WinForms 框架依赖 Windows
3. **无撤销功能** - 删除曲线后无法撤销
4. **双击删除策略简单** - 双击删除最后添加的曲线，未实现精确点击删除

## 数据格式要求

- CSV 文件，GBK 或 UTF-8 编码
- 第一行（或指定行）为表头（字段名）
- 字段名支持中文、英文、数字、下划线
- 典型字段：序号、俯仰角、横滚角、航向角、时戳等
- `时戳`字段默认作为 X 轴（整数）
- 典型文件包含 400+ 列数据字段
