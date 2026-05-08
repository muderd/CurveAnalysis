# 贡献指南

感谢你对 Flight Log Curve Analyzer 的关注！

## 如何贡献

### 报告 Bug

1. 在 [Issues](../../issues) 页面点击 "New Issue"
2. 选择 "Bug Report" 模板
3. 填写：
   - 问题描述
   - 复现步骤
   - 期望行为
   - 实际行为
   - CSV 文件格式示例（脱敏后）

### 提交功能建议

1. 在 [Issues](../../issues) 页面点击 "New Issue"
2. 选择 "Feature Request" 模板
3. 描述你想要的功能和使用场景

### 提交代码

1. Fork 本仓库
2. 创建功能分支：`git checkout -b feature/your-feature`
3. 提交更改
4. 发起 Pull Request

## 开发环境

- **IDE**: Visual Studio 2022 或 VS Code + C# Dev Kit
- **SDK**: .NET 10
- **依赖**: OxyPlot.Core 2.2.0 + OxyPlot.WindowsForms 2.2.0

## 编译

```bash
cd CurveAnalysis_CSharp
dotnet build
```

发布单文件 EXE：

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o bin/publish
```

## 代码规范

- 单文件结构（Program.cs 包含所有代码）
- 中文注释
- 方法命名：PascalCase
- 私有字段：_camelCase

## 项目结构

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
└── CHANGELOG.md                # 更新日志
```
