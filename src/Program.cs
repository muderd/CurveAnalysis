using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace CurveAnalysis_CSharp;

static class Program
{
    [STAThread]
    static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

// ═══════════════════════════════════════════════════════════════════════
// CSV读取器
// ═══════════════════════════════════════════════════════════════════════
public static class CsvReader
{
    public static string DetectEncoding(string path)
    {
        byte[] head = File.ReadAllBytes(path).Take(8192).ToArray();
        if (head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF) return "utf-8-sig";
        try { var gbk = Encoding.GetEncoding("gb18030"); var text = gbk.GetString(head); if (text.Split('\n')[0].Any(c => c >= 0x4e00 && c <= 0x9fff)) return "gb18030"; } catch { }
        return "gb18030";
    }
    public static List<string> ReadHeaders(string path, int headerRow = 1)
    {
        if (Path.GetExtension(path).ToLower() != ".csv") return new();
        var enc = DetectEncoding(path);
        using var sr = new StreamReader(path, Encoding.GetEncoding(enc));
        for (int i = 1; i < headerRow && !sr.EndOfStream; i++) sr.ReadLine();
        var line = sr.ReadLine();
        return line == null ? new() : ParseLine(line).Where(h => !string.IsNullOrWhiteSpace(h)).ToList();
    }
    public static double[] ReadColumn(string path, string column, string enc, int headerRow = 1, int dataStartRow = 2)
    {
        var result = new List<double>();
        using var sr = new StreamReader(path, Encoding.GetEncoding(enc));
        for (int i = 1; i < headerRow && !sr.EndOfStream; i++) sr.ReadLine();
        var headerLine = sr.ReadLine();
        if (headerLine == null) return Array.Empty<double>();
        var headers = ParseLine(headerLine);
        int colIdx = -1;
        for (int i = 0; i < headers.Length; i++) { if (headers[i].Trim() == column) { colIdx = i; break; } }
        if (colIdx < 0) return Array.Empty<double>();
        for (int i = headerRow + 1; i < dataStartRow && !sr.EndOfStream; i++) sr.ReadLine();
        while (!sr.EndOfStream) { var line = sr.ReadLine(); if (string.IsNullOrEmpty(line)) continue; var parts = ParseLine(line); if (colIdx < parts.Length && double.TryParse(parts[colIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) result.Add(d); else result.Add(double.NaN); }
        return result.ToArray();
    }
    private static string[] ParseLine(string line)
    {
        var result = new List<string>(); bool inQuote = false; var sb = new StringBuilder();
        foreach (var c in line) { if (c == '"') { inQuote = !inQuote; continue; } if (c == ',' && !inQuote) { result.Add(sb.ToString()); sb.Clear(); continue; } sb.Append(c); }
        result.Add(sb.ToString()); return result.ToArray();
    }
}

public class FileManager
{
    public string FilePath { get; }
    public string FileName { get; }
    public string FileEncoding { get; }
    public List<string> Headers { get; }
    public int HeaderRow { get; set; } = 1;
    public int DataStartRow { get; set; } = 2;
    public Dictionary<string, double[]> Cache { get; } = new();
    public FileManager(string path)
    {
        FilePath = path; FileName = Path.GetFileName(path);
        FileEncoding = CsvReader.DetectEncoding(path);
        Headers = CsvReader.ReadHeaders(path, HeaderRow);
    }
    public double[] GetData(string col)
    {
        if (Cache.TryGetValue(col, out var c)) return c;
        var d = CsvReader.ReadColumn(FilePath, col, FileEncoding, HeaderRow, DataStartRow);
        Cache[col] = d; return d;
    }
    public void Reload() { Cache.Clear(); Headers.Clear(); Headers.AddRange(CsvReader.ReadHeaders(FilePath, HeaderRow)); }
}

public class SettingsDialog : Form
{
    public int CsvHeaderRow { get; set; } = 1;
    public int CsvDataStartRow { get; set; } = 2;
    private readonly NumericUpDown _nudHeader, _nudData;
    private readonly Label _previewLabel;
    private readonly string _filePath, _encoding;
    public SettingsDialog(string filePath)
    {
        _filePath = filePath; _encoding = CsvReader.DetectEncoding(filePath);
        Text = "CSV 表头设置"; Size = new Size(450, 280); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(10) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "表头所在行:", AutoSize = true, Margin = new Padding(0, 8, 0, 0) }, 0, 0);
        _nudHeader = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1, Width = 80 }; _nudHeader.ValueChanged += (_, _) => UpdatePreview();
        layout.Controls.Add(_nudHeader, 1, 0);
        layout.Controls.Add(new Label { Text = "数据起始行:", AutoSize = true, Margin = new Padding(0, 8, 0, 0) }, 0, 1);
        _nudData = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 2, Width = 80 }; _nudData.ValueChanged += (_, _) => UpdatePreview();
        layout.Controls.Add(_nudData, 1, 1);
        layout.Controls.Add(new Label { Text = "预览:", AutoSize = true, Margin = new Padding(0, 8, 0, 0) }, 0, 2);
        _previewLabel = new Label { AutoSize = false, Width = 350, Height = 80, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 9) };
        layout.Controls.Add(_previewLabel, 1, 2);
        var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40 };
        btnPanel.Controls.Add(new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 80 });
        btnPanel.Controls.Add(new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 80 });
        layout.Controls.Add(btnPanel, 0, 3); layout.SetColumnSpan(btnPanel, 2);
        Controls.Add(layout); UpdatePreview();
    }
    private void UpdatePreview()
    {
        CsvHeaderRow = (int)_nudHeader.Value; CsvDataStartRow = (int)_nudData.Value;
        try
        {
            using var sr = new StreamReader(_filePath, Encoding.GetEncoding(_encoding));
            var lines = new List<string>();
            for (int i = 0; i < CsvDataStartRow + 2 && !sr.EndOfStream; i++) lines.Add(sr.ReadLine() ?? "");
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                string marker = i + 1 == CsvHeaderRow ? " <-- 表头行" : i + 1 == CsvDataStartRow ? " <-- 数据开始" : "";
                sb.AppendLine($"L{i + 1}: {lines[i].Substring(0, Math.Min(60, lines[i].Length))}{marker}");
            }
            _previewLabel.Text = sb.ToString();
        }
        catch { _previewLabel.Text = "无法预览"; }
    }
}

// ═══════════════════════════════════════════════════════════════════════
// 浮动窗口
// ═══════════════════════════════════════════════════════════════════════
public class FloatingChartForm : Form
{
    public PlotView PlotView { get; }
    public PlotModel PlotModel { get; }

    // 接管已有的 PlotView（不是新建），避免 OxyPlot 的 PlotModel 绑定冲突
    public FloatingChartForm(string title, PlotView existingView)
    {
        Text = $"📊 {title}";
        Size = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        PlotView = existingView;
        PlotModel = existingView.Model!;

        // 将 PlotView 从原容器中移除并添加到浮动窗口
        PlotView.Parent?.Controls.Remove(PlotView);
        PlotView.Dock = DockStyle.Fill;
        Controls.Add(PlotView);

        var ctxMenu = new ContextMenuStrip();
        ctxMenu.Items.Add("保存为图片...", null, (_, _) =>
        {
            using var dlg = new SaveFileDialog { Filter = "PNG 图片|*.png|所有文件|*.*", FileName = $"{title}.png" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var exporter = new PngExporter { Width = PlotView.ClientSize.Width * 2, Height = PlotView.ClientSize.Height * 2 };
                using var fs = File.Create(dlg.FileName);
                exporter.Export(PlotModel, fs);
            }
        });
        PlotView.ContextMenuStrip = ctxMenu;
    }
}

// ═══════════════════════════════════════════════════════════════════════
// 单个图表信息（含关闭按钮 + 弹出按钮）
// ═══════════════════════════════════════════════════════════════════════
public class ChartInfo
{
    public PlotModel PlotModel { get; }
    public PlotView PlotView { get; }
    public Panel Container { get; }
    public Label TitleLabel { get; }
    public Button CloseBtn { get; }
    public Button PopBtn { get; }
    public string Title { get; set; }
    public List<(FileManager file, string field, LineSeries series)> Plotted { get; } = new();
    public int ColorIndex { get; set; }
    public bool IsSelected { get; private set; }

    public ChartInfo(string title)
    {
        Title = title;

        // 关闭按钮
        CloseBtn = new Button
        {
            Text = "X",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(200, 50, 50),
            Size = new Size(26, 24),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Padding = new Padding(0)
        };
        CloseBtn.FlatAppearance.BorderSize = 0;

        // 弹出按钮
        PopBtn = new Button
        {
            Text = "POP",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Microsoft YaHei", 8, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(80, 130, 200),
            Size = new Size(36, 24),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Padding = new Padding(0)
        };
        PopBtn.FlatAppearance.BorderSize = 0;

        // 标题栏
        TitleLabel = new Label
        {
            Text = $"  {title}",
            BackColor = Color.FromArgb(60, 130, 200),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };

        // 标题栏面板（包含标题Label + 弹出按钮 + 关闭按钮）
        var titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(60, 130, 200)
        };
        titleBar.Controls.Add(TitleLabel);
        titleBar.Controls.Add(PopBtn);
        titleBar.Controls.Add(CloseBtn);

        // 图表
        PlotModel = new PlotModel { TitleFontSize = 11, TitlePadding = 6 };
        PlotModel.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.BottomCenter,
            LegendPlacement = LegendPlacement.Outside,
            LegendOrientation = LegendOrientation.Horizontal,
            LegendMargin = 10,
            LegendPadding = 3,
            LegendFontSize = 9
        });
        PlotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom, Title = "X",
            MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot
        });
        PlotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left, Title = "Y",
            MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot
        });

        PlotView = new PlotView { Model = PlotModel, Dock = DockStyle.Fill };
        PlotView.AllowDrop = true;
        PlotView.DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.Text) == true) e.Effect = DragDropEffects.Copy; };

        // 右键菜单：保存图片
        var ctxMenu = new ContextMenuStrip();
        ctxMenu.Items.Add("保存为图片...", null, (_, _) => SaveChartImage());
        PlotView.ContextMenuStrip = ctxMenu;

        // 容器
        Container = new Panel { Dock = DockStyle.None, BackColor = Color.FromArgb(240, 240, 240) };
        Container.Controls.Add(PlotView);
        Container.Controls.Add(titleBar);
    }

    /// <summary>更新标题栏显示（含曲线数）</summary>
    public void UpdateTitle()
    {
        TitleLabel.Text = Plotted.Count > 0
            ? $"  {Title}  ({Plotted.Count} 条曲线)"
            : $"  {Title}";
    }

    /// <summary>设置选中/未选中状态（蓝色边框高亮）</summary>
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (selected)
        {
            Container.BorderStyle = BorderStyle.FixedSingle;
            TitleLabel.BackColor = Color.FromArgb(30, 100, 180);
        }
        else
        {
            Container.BorderStyle = BorderStyle.None;
            TitleLabel.BackColor = Color.FromArgb(60, 130, 200);
        }
    }

    private void SaveChartImage()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png|所有文件|*.*",
            FileName = $"{Title}.png"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var exporter = new PngExporter
            {
                Width = PlotView.ClientSize.Width * 2,
                Height = PlotView.ClientSize.Height * 2
            };
            using var fs = File.Create(dlg.FileName);
            exporter.Export(PlotModel, fs);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════
// 常用预设
// ═══════════════════════════════════════════════════════════════════════
public class FieldPreset
{
    public string Name { get; set; } = "";
    public List<string> Fields { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════
// 主窗口 - V16 全功能版
// ═══════════════════════════════════════════════════════════════════════
public class MainForm : Form
{
    private readonly TreeView _fileList, _fieldTree;
    private readonly TextBox _searchBox;
    private readonly ComboBox _xFieldCombo, _chartSelector;
    private readonly NumericUpDown _xGranBox;
    private readonly Label _statusLabel, _lblA, _lblB, _lblC, _lblD;
    private readonly Panel _leftPanel, _rightPanel, _chartsContainer;

    private readonly List<FileManager> _files = new();
    private readonly List<ChartInfo> _charts = new();
    private int _chartCounter;
    private List<TreeNode> _allFieldNodes = new();
    private int _selectedFileIndex;

    // V16 网格布局
    private int _gridRows = 1, _gridCols = 1;

    // V16 基线对比
    private bool _baselineActive;
    private double _lastBaselineX = double.NaN;

    private static readonly OxyColor[] Palette = {
        OxyColor.FromRgb(0, 100, 200),
        OxyColor.FromRgb(220, 80, 0),
        OxyColor.FromRgb(0, 150, 60),
        OxyColor.FromRgb(200, 0, 0),
        OxyColor.FromRgb(130, 0, 180),
        OxyColor.FromRgb(160, 80, 0),
        OxyColor.FromRgb(200, 0, 140),
        OxyColor.FromRgb(80, 80, 80),
        OxyColor.FromRgb(100, 140, 0),
        OxyColor.FromRgb(0, 170, 180),
        OxyColor.FromRgb(180, 0, 80),
        OxyColor.FromRgb(0, 60, 140),
    };
    private const string TsCol = "时戳";
    private const int DsThresh = 8000;
    private const int SplitRatio = 30;
    private int _currentTheme;

    public MainForm()
    {
        Text = "曲线分析工具 V16";
        Size = new Size(1600, 1000);
        MinimumSize = new Size(1200, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei", 10);

        // ════════ Toolbar ════════
        var tb = new ToolStrip { Font = new Font("Microsoft YaHei", 11) };
        tb.Items.Add(new ToolStripButton("添加文件", null, OnAddFiles));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(new ToolStripButton("+ 新建图表", null, (_, _) => AddNewChart()));
        tb.Items.Add(new ToolStripButton("删除当前图", null, (_, _) => RemoveActiveChart()));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(new ToolStripButton("清除当前图", null, (_, _) => ClearActiveChart()));
        tb.Items.Add(new ToolStripButton("清除所有图", null, (_, _) => ClearAllCharts()));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(new ToolStripButton("全部叠到一图", null, (_, _) => MergeAllToOverlay()));
        tb.Items.Add(new ToolStripButton("按文件分图", null, (_, _) => SplitByFile()));
        tb.Items.Add(new ToolStripSeparator());
        // 网格布局控件
        tb.Items.Add(new ToolStripLabel("布局:"));
        var rowsBox = new ToolStripTextBox { Size = new Size(30, 25), Text = "1" };
        tb.Items.Add(rowsBox);
        tb.Items.Add(new ToolStripLabel("行 x"));
        var colsBox = new ToolStripTextBox { Size = new Size(30, 25), Text = "1" };
        tb.Items.Add(colsBox);
        tb.Items.Add(new ToolStripLabel("列"));
        tb.Items.Add(new ToolStripButton("应用布局", null, (_, _) =>
        {
            if (int.TryParse(rowsBox.Text, out var r) && int.TryParse(colsBox.Text, out var c) && r > 0 && c > 0)
            {
                _gridRows = r;
                _gridCols = c;
                RelayoutCharts();
                _statusLabel.Text = $"布局已设置: {_gridRows} 行 x {_gridCols} 列";
            }
        }));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(new ToolStripButton("基线对比", null, (_, _) => ToggleBaseline()));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(new ToolStripButton("重置视图", null, (_, _) => ResetView()));
        Controls.Add(tb);

        // ════════ Status bar ════════
        Controls.Add(_statusLabel = new Label { Dock = DockStyle.Bottom, Height = 28, Text = "就绪", Font = new Font("Microsoft YaHei", 10) });

        // ════════ 左面板 ════════
        _leftPanel = new Panel { BackColor = Color.White };
        Controls.Add(_leftPanel);

        _lblA = new Label { Text = " [A] 已加载文件", BackColor = Color.FromArgb(210, 225, 245), Font = new Font("Microsoft YaHei", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _leftPanel.Controls.Add(_lblA);

        _fileList = new TreeView { ShowLines = true, FullRowSelect = true, HideSelection = false, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Microsoft YaHei", 10) };
        _fileList.NodeMouseClick += FileList_Click;
        _fileList.AllowDrop = true;
        _fileList.DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy; };
        _fileList.DragDrop += (_, e) =>
        {
            var files = e.Data?.GetData(DataFormats.FileDrop) as string[];
            if (files == null) return;
            foreach (var path in files)
            {
                if (_files.Any(f => f.FilePath == path)) continue;
                using var dlg = new SettingsDialog(path);
                if (dlg.ShowDialog() != DialogResult.OK) continue;
                var fm = new FileManager(path) { HeaderRow = dlg.CsvHeaderRow, DataStartRow = dlg.CsvDataStartRow };
                fm.Reload();
                if (fm.Headers.Count == 0) { MessageBox.Show($"无法读取: {Path.GetFileName(path)}"); continue; }
                _files.Add(fm);
            }
            _selectedFileIndex = Math.Max(0, _files.Count - 1);
            RefreshUI();
        };
        _leftPanel.Controls.Add(_fileList);

        _lblB = new Label { Text = " [B] 字段搜索", BackColor = Color.FromArgb(210, 225, 245), Font = new Font("Microsoft YaHei", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _leftPanel.Controls.Add(_lblB);

        _searchBox = new TextBox { PlaceholderText = "输入字段名关键字...", Font = new Font("Microsoft YaHei", 10) };
        _searchBox.TextChanged += (_, _) => FilterFields(_searchBox.Text);
        _leftPanel.Controls.Add(_searchBox);

        _lblC = new Label { Text = " [C] 字段列表", BackColor = Color.FromArgb(210, 225, 245), Font = new Font("Microsoft YaHei", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _leftPanel.Controls.Add(_lblC);

        _fieldTree = new TreeView { ShowLines = true, FullRowSelect = true, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Microsoft YaHei", 10) };
        _fieldTree.NodeMouseDoubleClick += (_, e) =>
        {
            if (e.Node?.Tag is (string fp, string field))
            {
                var chart = GetActiveChart();
                if (chart != null) PlotField(chart, fp, field);
            }
        };
        _fieldTree.ItemDrag += (_, e) =>
        {
            if (e.Item is TreeNode n && n.Tag is (string fp, string field))
                DoDragDrop($"{fp}\t{field}", DragDropEffects.Copy);
        };
        _leftPanel.Controls.Add(_fieldTree);

        // ════════ 右面板 ════════
        _rightPanel = new Panel { BackColor = Color.White };
        Controls.Add(_rightPanel);

        // 控制栏
        var ctrlPanel = new Panel { BackColor = Color.FromArgb(235, 245, 235) };
        var lblFont = new Font("Microsoft YaHei", 9);
        int cy = 8;

        // X轴选择
        var xLabel = new Label { Text = "X轴:", AutoSize = true, Location = new Point(6, cy), Font = lblFont };
        _xFieldCombo = new ComboBox { Location = new Point(56, cy - 1), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = lblFont };
        _xFieldCombo.SelectedIndexChanged += (_, _) => ReplotAllCharts();
        ctrlPanel.Controls.Add(xLabel);
        ctrlPanel.Controls.Add(_xFieldCombo);

        // X粒度
        var granLabel = new Label { Text = "粒度:", AutoSize = true, Location = new Point(216, cy), Font = lblFont };
        _xGranBox = new NumericUpDown { Location = new Point(266, cy - 1), Width = 70, Minimum = 0, Maximum = 100000, Value = 0, Font = lblFont };
        _xGranBox.ValueChanged += (_, _) => ApplyXGranularityAll();
        var granHint = new Label { Text = "(0=自动)", AutoSize = true, Location = new Point(340, cy), Font = new Font("Microsoft YaHei", 8), ForeColor = Color.Gray };
        ctrlPanel.Controls.Add(granLabel);
        ctrlPanel.Controls.Add(_xGranBox);
        ctrlPanel.Controls.Add(granHint);

        // 背景色
        var bgLabel = new Label { Text = "背景:", AutoSize = true, Location = new Point(412, cy), Font = lblFont };
        var bgCombo = new ComboBox { Location = new Point(458, cy - 1), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, Font = lblFont };
        bgCombo.Items.AddRange(new object[] { "白色", "浅灰", "黑色", "深灰" });
        bgCombo.SelectedIndex = 0;
        bgCombo.SelectedIndexChanged += (_, _) => { _currentTheme = bgCombo.SelectedIndex; ApplyBackgroundAll(bgCombo.SelectedIndex); };
        ctrlPanel.Controls.Add(bgLabel);
        ctrlPanel.Controls.Add(bgCombo);

        // 目标图表选择
        _lblD = new Label { Text = "目标:", AutoSize = true, Location = new Point(548, cy), Font = lblFont };
        _chartSelector = new ComboBox { Location = new Point(594, cy - 1), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Font = lblFont };
        _chartSelector.SelectedIndexChanged += (_, _) =>
        {
            for (int i = 0; i < _charts.Count; i++)
                _charts[i].SetSelected(i == _chartSelector.SelectedIndex);
        };
        ctrlPanel.Controls.Add(_lblD);
        ctrlPanel.Controls.Add(_chartSelector);
        _rightPanel.Controls.Add(ctrlPanel);

        // 图表容器（支持滚动）
        _chartsContainer = new Panel { BackColor = Color.FromArgb(250, 250, 250), AutoScroll = true };
        _rightPanel.Controls.Add(_chartsContainer);

        // ── 拖拽到图表容器 ──
        _chartsContainer.AllowDrop = true;
        _chartsContainer.DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.Text) == true) e.Effect = DragDropEffects.Copy; };
        _chartsContainer.DragDrop += (_, e) =>
        {
            var text = e.Data?.GetData(DataFormats.Text) as string;
            if (text != null)
            {
                var chart = GetActiveChart();
                if (chart != null)
                {
                    var parts = text.Split('\t');
                    if (parts.Length == 2) PlotField(chart, parts[0], parts[1]);
                }
            }
        };

        // ── 绑定Resize事件 ──
        Resize += (_, _) => DoLayout();

        // 初始布局 + 创建第一个图表
        Shown += (_, _) =>
        {
            DoLayout();
            AddNewChart();
        };

        // 加载预设
        LoadPresets();
    }

    // ════════════════ 图表管理（多图） ════════════════

    private ChartInfo? GetActiveChart()
    {
        if (_chartSelector.SelectedIndex >= 0 && _chartSelector.SelectedIndex < _charts.Count)
            return _charts[_chartSelector.SelectedIndex];
        return _charts.Count > 0 ? _charts[^1] : null;
    }

    private void AddNewChart()
    {
        _chartCounter++;
        var chart = new ChartInfo($"图表{_chartCounter}");

        // 双击图表删除最后一条曲线
        chart.PlotView.MouseDoubleClick += (_, _) =>
        {
            if (chart.Plotted.Count > 0)
            {
                var last = chart.Plotted[^1];
                chart.PlotModel.Series.Remove(last.series);
                chart.Plotted.Remove(last);
                chart.UpdateTitle();
                chart.PlotModel.InvalidatePlot(true);
                _statusLabel.Text = $"已删除: {last.field}";
            }
        };

        // 单击图表显示交叉点数据
        chart.PlotView.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || chart.Plotted.Count == 0) return;
            ShowClickData(chart, e.Location);
        };

        // 关闭按钮
        chart.CloseBtn.Click += (_, _) => RemoveChart(chart);

        // 弹出按钮
        chart.PopBtn.Click += (_, _) => PopOutChart(chart);

        // 基线绑定
        if (_baselineActive) AttachBaseline(chart);

        _charts.Add(chart);
        _chartsContainer.Controls.Add(chart.Container);
        RefreshChartSelector();
        _chartSelector.SelectedIndex = _charts.Count - 1;
        RelayoutCharts();
        _statusLabel.Text = $"已创建 {chart.Title}";
    }

    private void RemoveChart(ChartInfo chart)
    {
        if (_charts.Count <= 1)
        {
            // 删除所有图表后自动创建新的（V15 自动补充）
            _charts.Remove(chart);
            _chartsContainer.Controls.Remove(chart.Container);
            chart.Container.Dispose();
            RefreshChartSelector();
            AddNewChart();
            _statusLabel.Text = "已自动补充新图表";
            return;
        }
        int idx = _charts.IndexOf(chart);
        _charts.Remove(chart);
        _chartsContainer.Controls.Remove(chart.Container);
        chart.Container.Dispose();
        RefreshChartSelector();
        if (idx >= _charts.Count) _chartSelector.SelectedIndex = _charts.Count - 1;
        else _chartSelector.SelectedIndex = idx;
        RelayoutCharts();
        _statusLabel.Text = $"已删除 {chart.Title}";
    }

    private void RemoveActiveChart()
    {
        var chart = GetActiveChart();
        if (chart != null) RemoveChart(chart);
    }

    private void ClearActiveChart()
    {
        var chart = GetActiveChart();
        if (chart == null) return;
        chart.PlotModel.Series.Clear();
        chart.Plotted.Clear();
        chart.ColorIndex = 0;
        chart.UpdateTitle();
        chart.PlotModel.InvalidatePlot(true);
    }

    private void ClearAllCharts()
    {
        foreach (var chart in _charts)
        {
            chart.PlotModel.Series.Clear();
            chart.Plotted.Clear();
            chart.ColorIndex = 0;
            chart.UpdateTitle();
            chart.PlotModel.InvalidatePlot(true);
        }
    }

    private void ResetView()
    {
        foreach (var chart in _charts)
        {
            foreach (var axis in chart.PlotModel.Axes) axis.Reset();
            chart.PlotModel.InvalidatePlot(true);
        }
    }

    private void RefreshChartSelector()
    {
        int prev = _chartSelector.SelectedIndex;
        _chartSelector.Items.Clear();
        foreach (var c in _charts) _chartSelector.Items.Add(c.Title);
        if (prev >= 0 && prev < _chartSelector.Items.Count) _chartSelector.SelectedIndex = prev;
        else if (_chartSelector.Items.Count > 0) _chartSelector.SelectedIndex = 0;
    }

    private void RelayoutCharts()
    {
        if (_charts.Count == 0) return;

        int w = _chartsContainer.ClientSize.Width - (_chartsContainer.AutoScroll ? SystemInformation.VerticalScrollBarWidth : 0);
        int h = _chartsContainer.ClientSize.Height;

        if (_gridRows <= 0) _gridRows = 1;
        if (_gridCols <= 0) _gridCols = 1;

        int totalCells = _gridRows * _gridCols;
        int chartW = w / _gridCols;
        int chartH = h / _gridRows;

        for (int i = 0; i < _charts.Count; i++)
        {
            int row = i / _gridCols;
            int col = i % _gridCols;
            if (row >= _gridRows) break;
            _charts[i].Container.Location = new Point(col * chartW, row * chartH);
            _charts[i].Container.Size = new Size(chartW, chartH);
        }

        int totalH = _gridRows * chartH;
        _chartsContainer.AutoScrollMinSize = new Size(0, totalH);
    }

    // ════════════════ V15：全部叠到一图 ════════════════

    private void MergeAllToOverlay()
    {
        if (_charts.Count == 0) AddNewChart();
        var target = _charts[0];
        // 收集所有图表的已绘曲线信息
        var allPlotted = new List<(string fp, string field)>();
        foreach (var chart in _charts)
        {
            foreach (var p in chart.Plotted)
                allPlotted.Add((p.file.FilePath, p.field));
        }
        // 清除所有图表
        foreach (var chart in _charts)
        {
            chart.PlotModel.Series.Clear();
            chart.Plotted.Clear();
            chart.ColorIndex = 0;
            chart.UpdateTitle();
            chart.PlotModel.InvalidatePlot(true);
        }
        // 全部画到第一个图表
        foreach (var (fp, field) in allPlotted)
            PlotField(target, fp, field);
        // 删除多余的图表
        while (_charts.Count > 1)
        {
            var last = _charts[^1];
            _charts.Remove(last);
            _chartsContainer.Controls.Remove(last.Container);
            last.Container.Dispose();
        }
        RefreshChartSelector();
        _chartSelector.SelectedIndex = 0;
        RelayoutCharts();
        _statusLabel.Text = $"已合并 {allPlotted.Count} 条曲线到第一个图表";
    }

    // ════════════════ V15：按文件分图 ════════════════

    private void SplitByFile()
    {
        if (_files.Count == 0) { MessageBox.Show("请先加载文件"); return; }

        // 分图前先收集当前已绘制的字段，按文件路径分组
        var plottedByFile = new Dictionary<string, List<string>>();
        foreach (var chart in _charts)
        {
            foreach (var p in chart.Plotted)
            {
                var fp = p.file.FilePath;
                if (!plottedByFile.ContainsKey(fp))
                    plottedByFile[fp] = new List<string>();
                if (!plottedByFile[fp].Contains(p.field))
                    plottedByFile[fp].Add(p.field);
            }
        }

        // 清除现有图表
        foreach (var chart in _charts)
        {
            _chartsContainer.Controls.Remove(chart.Container);
            chart.Container.Dispose();
        }
        _charts.Clear();
        _chartCounter = 0;

        // 为每个文件创建独立图表，只包含该文件已绘制的字段
        for (int fi = 0; fi < _files.Count; fi++)
        {
            var file = _files[fi];
            _chartCounter++;
            var chart = new ChartInfo($"图表{_chartCounter}");

            chart.PlotView.MouseDoubleClick += (_, _) =>
            {
                if (chart.Plotted.Count > 0)
                {
                    var last = chart.Plotted[^1];
                    chart.PlotModel.Series.Remove(last.series);
                    chart.Plotted.Remove(last);
                    chart.UpdateTitle();
                    chart.PlotModel.InvalidatePlot(true);
                    _statusLabel.Text = $"已删除: {last.field}";
                }
            };
            chart.CloseBtn.Click += (_, _) => RemoveChart(chart);
            chart.PopBtn.Click += (_, _) => PopOutChart(chart);
            if (_baselineActive) AttachBaseline(chart);

            // 只绘入该文件之前已绘制的字段
            if (plottedByFile.TryGetValue(file.FilePath, out var fields))
            {
                foreach (var header in fields)
                    PlotField(chart, file.FilePath, header);
            }

            _charts.Add(chart);
            _chartsContainer.Controls.Add(chart.Container);
        }
        RefreshChartSelector();
        if (_charts.Count > 0) _chartSelector.SelectedIndex = 0;
        RelayoutCharts();
        _statusLabel.Text = $"已按文件分图: {_files.Count} 个文件";
    }

    // ════════════════ V15：智能选中 ════════════════

    private void AutoSelectChartForFile(int fileIndex)
    {
        if (_charts.Count == 0) return;
        // 查找已包含该文件曲线的图表
        for (int i = 0; i < _charts.Count; i++)
        {
            if (_charts[i].Plotted.Any(p => _files.IndexOf(p.file) == fileIndex))
            {
                _chartSelector.SelectedIndex = i;
                return;
            }
        }
        // 未找到则选中第一个图表
        _chartSelector.SelectedIndex = 0;
    }

    // ════════════════ V16：浮动窗口 ════════════════

    private readonly Dictionary<ChartInfo, FloatingChartForm> _floatingWindows = new();
    private readonly List<FieldPreset> _presets = new();
    private ListBox _presetList = null!;
    private const string PresetsFile = "field_presets.json";

    private void PopOutChart(ChartInfo chart)
    {
        if (_floatingWindows.TryGetValue(chart, out var existing))
        {
            existing.Show();
            existing.BringToFront();
            return;
        }

        // 将 PlotView 从 ChartInfo 容器中取出，放入浮动窗口
        var floatForm = new FloatingChartForm(chart.Title, chart.PlotView)
        {
            Owner = this
        };
        _floatingWindows[chart] = floatForm;

        // 在原容器中放一个占位标签
        var placeholder = new Label
        {
            Text = $"[{chart.Title} 已弹出]",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(230, 230, 230),
            Font = new Font("Microsoft YaHei", 12, FontStyle.Italic),
            ForeColor = Color.Gray
        };
        chart.Container.Controls.Add(placeholder);

        floatForm.FormClosed += (_, _) =>
        {
            // 浮动窗口关闭时，把 PlotView 放回原容器
            placeholder.Parent?.Controls.Remove(placeholder);
            placeholder.Dispose();
            chart.PlotView.Dock = DockStyle.Fill;
            chart.Container.Controls.Add(chart.PlotView);
            _floatingWindows.Remove(chart);
        };

        floatForm.Show();
        _statusLabel.Text = $"已弹出: {chart.Title}（关闭浮动窗口自动返回）";
    }

    // ════════════════ V16：基线对比 ════════════════

    private void ToggleBaseline()
    {
        _baselineActive = !_baselineActive;
        if (_baselineActive)
        {
            // 为所有图表附加基线
            foreach (var chart in _charts) AttachBaseline(chart);
            _statusLabel.Text = "基线对比已开启（红色竖线）";
        }
        else
        {
            // 移除所有图表的基线
            foreach (var chart in _charts) DetachBaseline(chart);
            // 清除交叉点标签
            foreach (var chart in _charts)
            {
                var oldLabels = chart.PlotModel.Annotations
                    .OfType<OxyPlot.Annotations.TextAnnotation>()
                    .Where(a => a.Tag as string == "baseline_label")
                    .ToList();
                foreach (var old in oldLabels) chart.PlotModel.Annotations.Remove(old);
                chart.PlotModel.InvalidatePlot(true);
            }
            _statusLabel.Text = "基线对比已关闭";
        }
    }

    private void AttachBaseline(ChartInfo chart)
    {
        if (!_baselineActive) return;

        // 基线系列 - 使用 AnnotationLine（红色竖线）
        var annotation = new OxyPlot.Annotations.LineAnnotation
        {
            Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
            X = 0,
            Color = OxyColors.Red,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.Solid
        };

        // 移除旧的基线标注
        var oldAnnotations = chart.PlotModel.Annotations
            .OfType<OxyPlot.Annotations.LineAnnotation>()
            .Where(a => a.Color == OxyColors.Red)
            .ToList();
        foreach (var old in oldAnnotations) chart.PlotModel.Annotations.Remove(old);

        chart.PlotModel.Annotations.Add(annotation);

        // 绑定鼠标事件用于拖动基线
        chart.PlotView.MouseMove += Baseline_MouseMove;
        chart.PlotView.MouseDown += Baseline_MouseDown;

        chart.PlotModel.InvalidatePlot(true);
    }

    private void DetachBaseline(ChartInfo chart)
    {
        var toRemove = chart.PlotModel.Annotations
            .OfType<OxyPlot.Annotations.LineAnnotation>()
            .Where(a => a.Color == OxyColors.Red)
            .ToList();
        foreach (var a in toRemove) chart.PlotModel.Annotations.Remove(a);
        chart.PlotView.MouseMove -= Baseline_MouseMove;
        chart.PlotView.MouseDown -= Baseline_MouseDown;
        chart.PlotModel.InvalidatePlot(true);
    }

    private void Baseline_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (sender is not PlotView pv) return;
        var sourceChart = _charts.FirstOrDefault(c => c.PlotView == pv);
        if (sourceChart == null) return;

        var axis = sourceChart.PlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
        if (axis == null) return;
        double dataX = axis.InverseTransform(e.X);

        // 同步更新所有图表的基线位置
        foreach (var chart in _charts)
        {
            var ann = chart.PlotModel.Annotations
                .OfType<OxyPlot.Annotations.LineAnnotation>()
                .FirstOrDefault(a => a.Color == OxyColors.Red);
            if (ann != null)
            {
                ann.X = dataX;
                chart.PlotModel.InvalidatePlot(true);
            }
        }
    }

    private void Baseline_MouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is not PlotView pv) return;
        var sourceChart = _charts.FirstOrDefault(c => c.PlotView == pv);
        if (sourceChart == null) return;

        var xAxis = sourceChart.PlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
        if (xAxis == null) return;
        double dataX = xAxis.InverseTransform(e.X);

        // 节流：X 变化超过 0.5% 才更新
        double range = Math.Abs(xAxis.ActualMaximum - xAxis.ActualMinimum);
        if (!double.IsNaN(_lastBaselineX) && Math.Abs(dataX - _lastBaselineX) < range * 0.005) return;
        _lastBaselineX = dataX;

        // 同步更新所有图表的基线 + 交叉点标签
        foreach (var chart in _charts)
        {
            // 更新红线位置
            var ann = chart.PlotModel.Annotations
                .OfType<OxyPlot.Annotations.LineAnnotation>()
                .FirstOrDefault(a => a.Color == OxyColors.Red);
            if (ann != null)
            {
                ann.X = dataX;
                chart.PlotModel.InvalidatePlot(true);
            }

            // 清除旧的交叉点标签
            var oldLabels = chart.PlotModel.Annotations
                .OfType<OxyPlot.Annotations.TextAnnotation>()
                .Where(a => a.Tag as string == "baseline_label")
                .ToList();
            foreach (var old in oldLabels) chart.PlotModel.Annotations.Remove(old);

            // 收集所有交叉点位置
            var points = new List<(int fileIdx, double xVal, double yVal)>();
            int fi = 1;
            foreach (var p in chart.Plotted)
            {
                var yData = p.file.GetData(p.field);
                if (yData.Length == 0) { fi++; continue; }
                var xData = GetXDataForFile(p.file, yData.Length);
                if (xData.Length == 0) { fi++; continue; }
                int idx = FindNearestIndex(xData, dataX);
                if (idx >= 0 && idx < yData.Length && !double.IsNaN(yData[idx]))
                    points.Add((fi, xData[idx], yData[idx]));
                fi++;
            }

            // 按Y值排序，检测重叠并纵向错开
            var yAxis = chart.PlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
            double yRange = (yAxis != null) ? Math.Abs(yAxis.ActualMaximum - yAxis.ActualMinimum) : 100;
            double minGap = yRange * 0.04;
            var sorted = points.OrderBy(p => p.yVal).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (Math.Abs(sorted[i].yVal - sorted[i - 1].yVal) < minGap)
                {
                    sorted[i] = (sorted[i].fileIdx, sorted[i].xVal, sorted[i - 1].yVal + minGap);
                }
            }

            // 添加标签
            foreach (var pt in sorted)
            {
                var label = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = $"数据{pt.fileIdx}: X({pt.xVal:F2}), Y({pt.yVal:F2})",
                    TextPosition = new DataPoint(pt.xVal, pt.yVal),
                    TextColor = OxyColors.Black,
                    Background = OxyColor.FromRgb(220, 255, 220),
                    Stroke = OxyColor.FromRgb(180, 220, 180),
                    StrokeThickness = 1,
                    Font = "Microsoft YaHei",
                    FontSize = 9,
                    Padding = new OxyPlot.OxyThickness(2),
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left,
                    Tag = "baseline_label"
                };
                chart.PlotModel.Annotations.Add(label);
            }

            chart.PlotModel.InvalidatePlot(true);
        }
    }

    private int FindNearestIndex(double[] xData, double target)
    {
        if (xData.Length == 0) return -1;
        int lo = 0, hi = xData.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (xData[mid] < target) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }

    private double[] GetXDataForFile(FileManager file, int yLen)
    {
        var xf = _xFieldCombo.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(xf) && xf != "(行号/序号)")
        {
            var d = file.GetData(xf);
            if (d.Length > 0) return d;
        }
        return Enumerable.Range(0, yLen).Select(i => (double)i).ToArray();
    }

    // ════════════════ 单击显示数据 ════════════════

    private void ShowClickData(ChartInfo chart, Point location)
    {
        var xAxis = chart.PlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
        if (xAxis == null) return;
        double dataX = xAxis.InverseTransform(location.X);

        // 清除旧的点击标签
        var oldLabels = chart.PlotModel.Annotations
            .OfType<OxyPlot.Annotations.TextAnnotation>()
            .Where(a => a.Tag as string == "click_label")
            .ToList();
        foreach (var old in oldLabels) chart.PlotModel.Annotations.Remove(old);

        // 收集交叉点
        var points = new List<(int idx, double xVal, double yVal)>();
        int fi = 1;
        foreach (var p in chart.Plotted)
        {
            var yData = p.file.GetData(p.field);
            if (yData.Length == 0) { fi++; continue; }
            var xData = GetXDataForFile(p.file, yData.Length);
            if (xData.Length == 0) { fi++; continue; }
            int idx = FindNearestIndex(xData, dataX);
            if (idx >= 0 && idx < yData.Length && !double.IsNaN(yData[idx]))
                points.Add((fi, xData[idx], yData[idx]));
            fi++;
        }

        // 排序并错开重叠
        var yAxis = chart.PlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
        double yRange = (yAxis != null) ? Math.Abs(yAxis.ActualMaximum - yAxis.ActualMinimum) : 100;
        double minGap = yRange * 0.04;
        var sorted = points.OrderBy(p => p.yVal).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            if (Math.Abs(sorted[i].yVal - sorted[i - 1].yVal) < minGap)
                sorted[i] = (sorted[i].idx, sorted[i].xVal, sorted[i - 1].yVal + minGap);
        }

        // 添加标签
        foreach (var pt in sorted)
        {
            var label = new OxyPlot.Annotations.TextAnnotation
            {
                Text = $"数据{pt.idx}: X({pt.xVal:F2}), Y({pt.yVal:F2})",
                TextPosition = new DataPoint(pt.xVal, pt.yVal),
                TextColor = OxyColors.Black,
                Background = OxyColor.FromRgb(220, 240, 255),
                Stroke = OxyColor.FromRgb(180, 200, 230),
                StrokeThickness = 1,
                Font = "Microsoft YaHei",
                FontSize = 9,
                Padding = new OxyPlot.OxyThickness(2),
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left,
                Tag = "click_label"
            };
            chart.PlotModel.Annotations.Add(label);
        }

        chart.PlotModel.InvalidatePlot(true);
    }

    // ════════════════ 布局 ════════════════

    private void DoLayout()
    {
        int tbH = Controls.OfType<ToolStrip>().FirstOrDefault()?.Height ?? 40;
        int sbH = _statusLabel.Height;
        int contentH = ClientSize.Height - tbH - sbH;
        int contentW = ClientSize.Width;
        int leftW = contentW * SplitRatio / 100;
        int rightW = contentW - leftW;

        _leftPanel.Location = new Point(0, tbH);
        _leftPanel.Size = new Size(leftW, contentH);
        DoLeftLayout(_leftPanel);

        _rightPanel.Location = new Point(leftW, tbH);
        _rightPanel.Size = new Size(rightW, contentH);
        DoRightLayout(_rightPanel);
    }

    private void DoLeftLayout(Panel p)
    {
        int w = p.ClientSize.Width;
        int y = 0;
        int lblH = 30;
        int fileListH = 160;
        int searchH = 32;

        _lblA.Location = new Point(0, y); _lblA.Size = new Size(w, lblH); y += lblH;
        _fileList.Location = new Point(0, y); _fileList.Size = new Size(w, fileListH); y += fileListH;
        _lblB.Location = new Point(0, y); _lblB.Size = new Size(w, lblH); y += lblH;
        _searchBox.Location = new Point(4, y + 4); _searchBox.Size = new Size(w - 8, 24); y += searchH;
        _lblC.Location = new Point(0, y); _lblC.Size = new Size(w, lblH); y += lblH;
        _fieldTree.Location = new Point(0, y); _fieldTree.Size = new Size(w, Math.Max(100, p.ClientSize.Height - y - 140));
        y += Math.Max(100, p.ClientSize.Height - y - 140);

        // 预设区
        var lblPreset = new Label { Text = " [E] 常用预设", BackColor = Color.FromArgb(210, 225, 245), Font = new Font("Microsoft YaHei", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Height = 26 };
        p.Controls.Add(lblPreset);
        lblPreset.Location = new Point(0, y); lblPreset.Size = new Size(w, 26); y += 26;

        _presetList = new ListBox { Font = new Font("Microsoft YaHei", 9), BorderStyle = BorderStyle.FixedSingle };
        _presetList.DoubleClick += (_, _) => ApplyPreset();
        _presetList.Location = new Point(0, y);
        _presetList.Size = new Size(w, 80);
        p.Controls.Add(_presetList);
        y += 80;

        var presetBtnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Height = 32, Location = new Point(0, y), AutoSize = false };
        var btnSavePreset = new Button { Text = "保存", Width = 58, Height = 28, Font = new Font("Microsoft YaHei", 9) };
        btnSavePreset.Click += (_, _) => SaveCurrentAsPreset();
        var btnLoadPreset = new Button { Text = "读取", Width = 58, Height = 28, Font = new Font("Microsoft YaHei", 9) };
        btnLoadPreset.Click += (_, _) => ApplyPreset();
        var btnDeletePreset = new Button { Text = "删除", Width = 58, Height = 28, Font = new Font("Microsoft YaHei", 9) };
        btnDeletePreset.Click += (_, _) => DeleteSelectedPreset();
        presetBtnPanel.Controls.Add(btnSavePreset);
        presetBtnPanel.Controls.Add(btnLoadPreset);
        presetBtnPanel.Controls.Add(btnDeletePreset);
        p.Controls.Add(presetBtnPanel);
    }

    private void DoRightLayout(Panel p)
    {
        int w = p.ClientSize.Width;
        int y = 0;
        int ctrlH = 42;

        // 控制栏
        var ctrlPanel = p.Controls.OfType<Panel>().FirstOrDefault(c => c.Controls.Contains(_xFieldCombo));
        if (ctrlPanel != null) { ctrlPanel.Location = new Point(0, y); ctrlPanel.Size = new Size(w, ctrlH); }
        y += ctrlH;

        // 图表容器 - 铺满剩余空间
        _chartsContainer.Location = new Point(0, y);
        _chartsContainer.Size = new Size(w, p.ClientSize.Height - y);

        RelayoutCharts();
    }

    // ════════════════ 文件操作 ════════════════

    private void OnAddFiles(object? s, EventArgs e)
    {
        using var d = new OpenFileDialog { Filter = "CSV|*.csv|All|*.*", Multiselect = true };
        if (d.ShowDialog() != DialogResult.OK) return;
        foreach (var path in d.FileNames)
        {
            if (_files.Any(f => f.FilePath == path)) continue;
            using var dlg = new SettingsDialog(path);
            if (dlg.ShowDialog() != DialogResult.OK) continue;
            var fm = new FileManager(path) { HeaderRow = dlg.CsvHeaderRow, DataStartRow = dlg.CsvDataStartRow };
            fm.Reload();
            if (fm.Headers.Count == 0) { MessageBox.Show($"无法读取: {Path.GetFileName(path)}"); continue; }
            _files.Add(fm);
        }
        _selectedFileIndex = Math.Max(0, _files.Count - 1);
        RefreshUI();
    }

    private void FileList_Click(object? s, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is int idx)
        {
            if (e.Button == MouseButtons.Right)
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add("更换文件", null, (_, _) => ReplaceFile(idx));
                menu.Items.Add("删除文件", null, (_, _) => RemoveFile(idx));
                menu.Show(Cursor.Position);
            }
            else if (e.Button == MouseButtons.Left)
            {
                _selectedFileIndex = idx;
                RefreshFileListHighlight();
                RefreshFieldList();
                AutoSelectChartForFile(idx);
            }
        }
    }

    private void ReplaceFile(int index)
    {
        if (index < 0 || index >= _files.Count) return;
        using var d = new OpenFileDialog { Filter = "CSV|*.csv|All|*.*", Multiselect = false };
        if (d.ShowDialog() != DialogResult.OK) return;
        using var dlg = new SettingsDialog(d.FileName);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var old = _files[index];
        // 从所有图表中移除旧文件的曲线
        foreach (var chart in _charts)
        {
            foreach (var p in chart.Plotted.Where(p => p.file == old).ToList())
            {
                chart.PlotModel.Series.Remove(p.series);
                chart.Plotted.Remove(p);
            }
            chart.UpdateTitle();
            chart.PlotModel.InvalidatePlot(true);
        }
        var fm = new FileManager(d.FileName) { HeaderRow = dlg.CsvHeaderRow, DataStartRow = dlg.CsvDataStartRow };
        fm.Reload();
        _files[index] = fm;
        RefreshUI();
    }

    private void RemoveFile(int index)
    {
        if (index < 0 || index >= _files.Count) return;
        var f = _files[index]; _files.RemoveAt(index);
        // 从所有图表中移除该文件的曲线
        foreach (var chart in _charts)
        {
            foreach (var p in chart.Plotted.Where(p => p.file == f).ToList())
            {
                chart.PlotModel.Series.Remove(p.series);
                chart.Plotted.Remove(p);
            }
            chart.UpdateTitle();
            chart.PlotModel.InvalidatePlot(true);
        }
        _selectedFileIndex = Math.Max(0, Math.Min(_selectedFileIndex, _files.Count - 1));
        RefreshUI();
    }

    private void RefreshUI() { RefreshFileList(); RefreshFieldList(); RefreshXAxisCombo(); FilterFields(_searchBox.Text); }

    private void RefreshFileList()
    {
        _fileList.Nodes.Clear();
        for (int i = 0; i < _files.Count; i++)
        {
            var f = _files[i];
            var slotLabel = i switch { 0 => "数据1", 1 => "数据2", _ => $"数据{i + 1}" };
            _fileList.Nodes.Add(new TreeNode($"{slotLabel}: {f.FileName} ({f.Headers.Count}列)") { Tag = i });
        }
        RefreshFileListHighlight();
    }

    private void RefreshFileListHighlight()
    {
        for (int i = 0; i < _fileList.Nodes.Count; i++)
        {
            var n = _fileList.Nodes[i];
            if (i == _selectedFileIndex) { n.NodeFont = new Font(_fileList.Font, FontStyle.Bold); n.ForeColor = Color.FromArgb(0, 80, 160); n.EnsureVisible(); }
            else { n.NodeFont = new Font(_fileList.Font, FontStyle.Regular); n.ForeColor = SystemColors.WindowText; }
        }
    }

    private void RefreshFieldList()
    {
        _fieldTree.Nodes.Clear();
        _allFieldNodes.Clear();
        if (_selectedFileIndex < 0 || _selectedFileIndex >= _files.Count) return;
        foreach (var field in _files[_selectedFileIndex].Headers)
        {
            var node = new TreeNode(field) { Tag = (_files[_selectedFileIndex].FilePath, field) };
            _fieldTree.Nodes.Add(node);
            _allFieldNodes.Add(node);
        }
    }

    private void RefreshXAxisCombo()
    {
        _xFieldCombo.Items.Clear();
        _xFieldCombo.Items.Add("(行号/序号)");
        if (_files.Count > 0)
        {
            var common = new HashSet<string>(_files[0].Headers);
            foreach (var f in _files.Skip(1)) common.IntersectWith(f.Headers);
            var cols = common.OrderBy(c => c).ToList();
            if (cols.Contains(TsCol)) { cols.Remove(TsCol); cols.Insert(0, TsCol); }
            foreach (var c in cols) _xFieldCombo.Items.Add(c);
        }
        _xFieldCombo.SelectedIndex = 0;
    }

    // ════════════════ 绘图 ════════════════

    private void PlotField(ChartInfo chart, string fp, string field)
    {
        var file = _files.FirstOrDefault(f => f.FilePath == fp);
        if (file == null) return;
        if (chart.Plotted.Any(p => p.file == file && p.field == field)) return;

        // 文件序号前缀，用于区分不同文件的曲线
        int fileIdx = _files.IndexOf(file);
        string prefix = fileIdx >= 0 ? $"数据{fileIdx + 1}" : "";

        var yData = file.GetData(field);
        if (yData.Length == 0) return;
        var xData = GetXData(file, yData.Length);
        int len = Math.Min(xData.Length, yData.Length);
        var xs = xData.Take(len).ToArray();
        var ys = yData.Take(len).ToArray();
        if (xs.Length > DsThresh)
        {
            int step = xs.Length / DsThresh;
            xs = xs.Where((_, i) => i % step == 0).ToArray();
            ys = ys.Where((_, i) => i % step == 0).ToArray();
        }

        var color = Palette[chart.ColorIndex % Palette.Length];
        chart.ColorIndex++;
        string legendText = string.IsNullOrEmpty(prefix) ? field : $"{prefix}: {field}";
        var series = new LineSeries { Title = legendText, Color = color, StrokeThickness = 2.5, MarkerType = MarkerType.None };
        for (int i = 0; i < xs.Length; i++)
        {
            if (!double.IsNaN(ys[i])) series.Points.Add(new DataPoint(xs[i], ys[i]));
        }

        chart.PlotModel.Series.Add(series);
        chart.Plotted.Add((file, field, series));
        chart.UpdateTitle();
        chart.PlotModel.InvalidatePlot(true);
        _statusLabel.Text = $"已绘制: {legendText} ({len} 点) → {chart.Title}";
    }

    private double[] GetXData(FileManager file, int yLen)
    {
        var xf = _xFieldCombo.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(xf) && xf != "(行号/序号)")
        {
            var d = file.GetData(xf);
            if (d.Length > 0) return d;
        }
        return Enumerable.Range(0, yLen).Select(i => (double)i).ToArray();
    }

    private void ReplotAllCharts()
    {
        foreach (var chart in _charts)
        {
            var plotted = chart.Plotted.Select(p => (p.file.FilePath, p.field)).ToList();
            chart.PlotModel.Series.Clear();
            chart.Plotted.Clear();
            chart.ColorIndex = 0;
            foreach (var (fp, field) in plotted) PlotField(chart, fp, field);
        }
    }

    private void ApplyXGranularityAll()
    {
        int gran = (int)_xGranBox.Value;
        foreach (var chart in _charts)
        {
            foreach (var axis in chart.PlotModel.Axes)
            {
                if (axis.Position == AxisPosition.Bottom && axis is LinearAxis la)
                {
                    if (gran > 0)
                    {
                        // 如果数据范围远大于粒度，自动放大粒度避免渲染问题
                        double range = la.ActualMaximum - la.ActualMinimum;
                        double step = gran;
                        if (range / gran > 500) step = range / 500;
                        la.MajorStep = step;
                        la.MinorStep = step / 5.0;
                    }
                    else
                    {
                        la.MajorStep = double.NaN;
                        la.MinorStep = double.NaN;
                    }
                }
            }
            chart.PlotModel.InvalidatePlot(true);
        }
    }

    // ════════════════ 预设管理 ════════════════

    private void SaveCurrentAsPreset()
    {
        var chart = GetActiveChart();
        if (chart == null || chart.Plotted.Count == 0) { _statusLabel.Text = "当前图表没有曲线"; return; }
        var name = ShowInputDialog("预设名称:", "保存预设", $"预设{_presets.Count + 1}");
        if (string.IsNullOrWhiteSpace(name)) return;
        var preset = new FieldPreset { Name = name, Fields = chart.Plotted.Select(p => $"{p.file.FilePath}\t{p.field}").ToList() };
        _presets.Add(preset);
        SavePresetsToFile();
        RefreshPresetList();
        _statusLabel.Text = $"已保存预设: {name}";
    }

    private void ApplyPreset()
    {
        if (_presetList.SelectedIndex < 0) return;
        var preset = _presets[_presetList.SelectedIndex];
        var chart = GetActiveChart();
        if (chart == null) return;
        foreach (var entry in preset.Fields)
        {
            var parts = entry.Split('\t');
            if (parts.Length == 2) PlotField(chart, parts[0], parts[1]);
        }
        _statusLabel.Text = $"已应用预设: {preset.Name}";
    }

    private void DeleteSelectedPreset()
    {
        if (_presetList.SelectedIndex < 0) return;
        _presets.RemoveAt(_presetList.SelectedIndex);
        SavePresetsToFile();
        RefreshPresetList();
    }

    private void RefreshPresetList()
    {
        _presetList.Items.Clear();
        foreach (var p in _presets) _presetList.Items.Add($"{p.Name} ({p.Fields.Count}条)");
    }

    private void SavePresetsToFile()
    {
        try { File.WriteAllText(PresetsFile, System.Text.Json.JsonSerializer.Serialize(_presets)); } catch { }
    }

    private void LoadPresets()
    {
        try
        {
            if (File.Exists(PresetsFile))
            {
                var loaded = System.Text.Json.JsonSerializer.Deserialize<List<FieldPreset>>(File.ReadAllText(PresetsFile));
                if (loaded != null) { _presets.AddRange(loaded); RefreshPresetList(); }
            }
        } catch { }
    }

    private static string ShowInputDialog(string title, string prompt, string defaultValue)
    {
        var form = new Form { Text = title, Size = new Size(320, 160), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var label = new Label { Text = prompt, Location = new Point(12, 14), AutoSize = true, Font = new Font("Microsoft YaHei", 10) };
        var textBox = new TextBox { Text = defaultValue, Location = new Point(12, 42), Width = 285, Font = new Font("Microsoft YaHei", 10) };
        var okBtn = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(110, 80), Width = 85, Height = 32, Font = new Font("Microsoft YaHei", 10) };
        var cancelBtn = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(205, 80), Width = 85, Height = 32, Font = new Font("Microsoft YaHei", 10) };
        form.Controls.AddRange(new Control[] { label, textBox, okBtn, cancelBtn });
        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;
        return form.ShowDialog() == DialogResult.OK ? textBox.Text : "";
    }

    private void FilterFields(string text)
    {
        text = text.ToLower().Trim();
        _fieldTree.Nodes.Clear();
        if (string.IsNullOrEmpty(text))
        {
            foreach (var node in _allFieldNodes) _fieldTree.Nodes.Add(node);
        }
        else
        {
            TreeNode? firstMatch = null;
            foreach (var node in _allFieldNodes)
            {
                if (node.Text.ToLower().Contains(text))
                {
                    _fieldTree.Nodes.Add(node);
                    if (firstMatch == null) firstMatch = node;
                }
            }
            if (firstMatch != null)
            {
                _fieldTree.SelectedNode = firstMatch;
                firstMatch.EnsureVisible();
            }
        }
    }

    // ════════════════ 背景色 ════════════════

    private void ApplyBackgroundAll(int theme)
    {
        OxyColor plotBg, plotGrid;
        Color containerBg;
        switch (theme)
        {
            case 1:
                plotBg = OxyColors.LightGray; plotGrid = OxyColors.Gray; containerBg = Color.LightGray; break;
            case 2:
                plotBg = OxyColors.Black; plotGrid = OxyColors.DarkGray; containerBg = Color.Black; break;
            case 3:
                plotBg = OxyColors.DimGray; plotGrid = OxyColors.Gray; containerBg = Color.DimGray; break;
            default:
                plotBg = OxyColors.White; plotGrid = OxyColors.LightGray; containerBg = Color.FromArgb(250, 250, 250); break;
        }
        foreach (var chart in _charts)
        {
            chart.PlotModel.Background = plotBg;
            foreach (var axis in chart.PlotModel.Axes)
            {
                axis.MajorGridlineColor = plotGrid;
                axis.MinorGridlineColor = plotGrid;
            }
            chart.Container.BackColor = containerBg;
            chart.PlotModel.TextColor = (theme >= 2) ? OxyColors.White : OxyColors.Black;
            chart.PlotModel.InvalidatePlot(true);
        }
    }
}
