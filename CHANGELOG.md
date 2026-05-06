# Changelog

## V16b (2026-05-06) - Bug Fix
- **Fix**: "按文件分图" now only shows previously plotted fields per file, instead of all fields
- Root cause: SplitByFile() was iterating all headers and plotting everything; now it collects currently plotted fields before clearing and only re-plots those

## V16 (2026-04-29)
- Curve thickening, high-contrast colors, search filter
- Save as image, file drag-drop, X-axis granularity
- Background theme switch, presets, floating window
- Grid layout, baseline comparison, click-to-show data
- Label anti-overlap

## V15 (2026-04-29)
- Merged overlay + multi-chart modes
- Split by file, merge all to one chart
- Auto legend prefix for data source

## V14 (2026-04-29)
- Multi-chart parallel display
- Independent chart management

## V13 (2026-04-29)
- Basic overlay functionality
- GBK/UTF-8 encoding support
- Header config dialog