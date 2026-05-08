# Flight Log Curve Analyzer

A lightweight CSV data visualization tool built for embedded/flight-controller engineers. Load CSV files, pick fields, and instantly plot time-series curves — all in a single portable EXE.

[![.NET](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)]()
[![License](https://img.shields.io/badge/License-MIT-green)]()

## Features

- **Zero setup** — Single self-contained EXE (~50MB), no installation needed
- **Multi-file** — Load multiple CSV files simultaneously, auto-detect GBK/UTF-8 encoding
- **Instant plotting** — Double-click any field to draw its time-series curve
- **Three modes** — Overlay (all curves on one chart), multi-chart (side-by-side), or mixed
- **Smart search** — Filter fields by keyword in real-time
- **Data tracing** — Legend auto-prefixes series with source label (`Data1:`, `Data2:`)
- **Large data** — Auto down-sampling handles 130MB+ CSV files smoothly
- **Interactive** — Scroll to zoom, right-click to pan, double-click to remove curves

## Quick Start

1. Download `CurveAnalysis_V16.exe` from [Releases](../../releases)
2. Double-click to run
3. Click **Add Files** → select your CSV → confirm header settings
4. Double-click any field in the list → curve appears on the chart

## Three Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| **Overlay** | All curves on one chart | Compare parameter trends |
| **Multi-chart** | Multiple independent charts | View different data types simultaneously |
| **Hybrid** | One-click merge / split | Flexible switching |

## Building

Requires .NET 10 SDK:

```bash
cd CurveAnalysis_CSharp
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -o bin/publish_v15
```

## Tech Stack

- C# / .NET 10 / WinForms
- [OxyPlot](https://github.com/oxyplot/oxyplot) 2.2.0 for charting
- Auto GBK/UTF-8 encoding detection

## License

MIT License
