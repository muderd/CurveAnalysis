# 飞控日志曲线分析工具 - 技术文档

## 项目概述

**名称：** 飞控日志曲线分析工具（叠加模式）  
**用途：** 加载多个飞控日志CSV文件，选择字段绘制时序曲线，支持叠加对比分析  
**技术栈：** C# WinForms + OxyPlot 2.2.0 + .NET 10  
**输出：** 自包含单文件exe（约50MB），无需安装Python/.NET运行时  

## 功能清单

### 核心功能
1. **CSV文件加载** - 支持GBK/UTF-8编码自动检测，懒加载表头和列数据
2. **表头配置** - 可配置表头行号和数据起始行，带预览
3. **字段列表** - 按文件分组显示，支持搜索过滤
4. **曲线绘制** - 双击或拖拽字段到图表区，每条曲线独立颜色
5. **X轴选择** - 默认"时戳"字段，可切换任意字段作为X轴
6. **图表交互** - 滚轮缩放、右键平移、双击删除曲线
7. **图例显示** - 底部水平排列，显示字段名
8. **文件管理** - 支持添加/替换/删除文件

### 辅助功能
- 搜索框自动滚动到第一个匹配项
- 切换文件后自动重新应用搜索过滤
- 文件列表左键选中、右键弹出菜单（更换/删除）
- 状态栏显示当前操作结果

## 界面布局

```
┌─────────────────────────────────────────────────────┐
│ [工具栏] 添加文件 | 清除图表 | 重置视图              │
├────────────────────┬────────────────────────────────┤
│ [A] 已加载文件     │ [D] X轴选择                    │
│   数据1: xxx.csv   │   X轴字段: [下拉框_________]  │
│   数据2: yyy.csv   ├────────────────────────────────┤
├────────────────────┤ [E] 图表区域                   │
│ [B] 字段搜索       │   (滚轮缩放/右键平移/双击删除) │
│   [输入关键字...]  │                                │
├────────────────────┤                                │
│ [C] 字段列表       │   图例（底部水平排列）         │
│   (双击或拖拽绘图) │                                │
└────────────────────┴────────────────────────────────┘
```

**布局比例：** 左面板30% | 右面板70%  
**实现方式：** 绝对定位 + Resize事件动态调整

## 关键技术实现

### 1. GBK编码检测
```csharp
// 读取前8KB，检查BOM，然后尝试GBK解码
// 如果解码后包含中文字符（Unicode 0x4e00-0x9fff），确认是GBK
byte[] head = File.ReadAllBytes(path).Take(8192).ToArray();
var gbk = Encoding.GetEncoding("gb18030");
var text = gbk.GetString(head);
if (text.Split('\n')[0].Any(c => c >= 0x4e00 && c <= 0x9fff))
    return "gb18030";
```

**重要：** .NET 8+需要在Main()中调用`Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`才能支持GBK编码。

### 2. 绝对定位布局
使用Resize事件动态计算每个控件的位置和大小：
```csharp
// 分栏比例：左30% 右70%
int leftW = contentW * SplitRatio / 100;
int rightW = contentW - leftW;

// 左面板内部布局
_lblA.Location = new Point(0, y); _lblA.Size = new Size(w, lblH);
_fileList.Location = new Point(0, y); _fileList.Size = new Size(w, fileListH);
// ... 依次向下排列
```

### 3. OxyPlot图表配置
```csharp
// 图例放在底部，水平排列，不占用图表空间
_plotModel.Legends.Add(new Legend {
    LegendPosition = LegendPosition.BottomCenter,
    LegendPlacement = LegendPlacement.Outside,
    LegendOrientation = LegendOrientation.Horizontal
});
```

### 4. 懒加载列数据
```csharp
// 首次访问时从磁盘读取，后续从缓存返回
public double[] GetData(string col)
{
    if (Cache.TryGetValue(col, out var c)) return c;
    var d = CsvReader.ReadColumn(FilePath, col, FileEncoding, HeaderRow, DataStartRow);
    Cache[col] = d; return d;
}
```

### 5. 降采样
当数据点超过8000个时，按步长抽样以保持渲染性能：
```csharp
if (xs.Length > DsThresh)
{
    int step = xs.Length / DsThresh;
    xs = xs.Where((_, i) => i % step == 0).ToArray();
    ys = ys.Where((_, i) => i % step == 0).ToArray();
}
```

### 6. 双击删除曲线
```csharp
private void PlotView_MouseDown(object? s, MouseEventArgs e)
{
    if (e.Clicks == 2 && _plotted.Count > 0)
    {
        // 双击删除最后添加的曲线
        var last = _plotted[^1];
        _plotModel.Series.Remove(last.series);
        _plotted.Remove(last);
        _plotModel.InvalidatePlot(true);
    }
}
```

## 文件结构

```
curve_analysis/
├── CurveAnalysis_CSharp/          # C#项目目录
│   ├── Program.cs                 # 主程序（所有代码在一个文件）
│   ├── CurveAnalysis_CSharp.csproj
│   ├── REQUIREMENTS.md
│   └── bin/publish_v12/
│       └── CurveAnalysis_CSharp.exe
├── CurveAnalysis_v12.exe          # 最终发布版本
├── TECHNICAL_DOC.md               # 本文档
└── README.md
```

## 编译和发布

```bash
# 编译
dotnet build -c Release

# 发布为自包含单文件exe
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o bin/publish_v12
```

## 已知限制

1. **仅支持CSV** - Excel（.xlsx）支持待实现
2. **无分屏模式** - 当前只有叠加模式，分屏模式待开发
3. **无撤销功能** - 删除曲线后无法撤销
4. **双击删除策略简单** - 当前双击删除最后添加的曲线，未实现精确点击删除

## 数据格式要求

- CSV文件，GBK或UTF-8编码
- 第一行（或指定行）为表头（字段名）
- 字段名支持中文、英文、数字、下划线
- 典型字段：序号、俯仰角、横滚角、航向角、时戳等
- `时戳`字段默认作为X轴（整数，如3260→52367）

## 依赖包

| 包名 | 版本 | 用途 |
|------|------|------|
| OxyPlot.Core | 2.2.0 | 图表核心 |
| OxyPlot.WindowsForms | 2.2.0 | WinForms集成 |

## 版本历史

- **v1.0** - 基础功能：文件加载、字段列表、曲线绘制
- **v2.0** - 修复GBK编码、添加表头配置对话框
- **v3.0** - 优化布局：区域标签、文件列表、搜索功能
- **v4.0** - 绝对定位布局、双击删除、完整注释
