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
// 主窗口 - V13 单图表版
// ═══════════════════════════════════════════════════════════════════════
public class MainForm : Form
{
    private readonly TreeView _fileList, _fieldTree;
    private readonly TextBox _searchBox;
    private readonly ComboBox _xFieldCombo;
    private readonly NumericUpDown _xGranBox;
    private readonly Label _statusLabel, _lblA, _lblB, _lblC;
    private readonly Panel _leftPanel, _rightPanel;

    private readonly List<FileManager> _files = new();
    private List<TreeNode> _allFieldNodes = new();
    private int _selectedFileIndex;

    // 单图表
    private PlotModel _plotModel = null!;
    private PlotView _plotView = null!;
    private readonly List<(FileManager file, string field, LineSeries series)> _plotted = new();
    private int _colorIndex;

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

    public MainForm()
    {
        Text = "CurveAnalysis V13";
        Size = new Size(1600, 1000);
        MinimumSize = new Size(1200, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei", 10);

        // ════════ Toolbar ════════
        var tb = new ToolStrip { Font = new Font("Microsoft YaHei", 11) };
        tb.Items.Add(new ToolStripButton("添加文件", null, OnAddFiles));
        tb.Items.Add(new ToolStripSeparator());
        tb.Items.Add(new ToolStripButton("清除曲线", null, (_, _) => ClearChart()));
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
                PlotField(fp, field);
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
        _xFieldCombo.SelectedIndexChanged += (_, _) => ReplotChart();
        ctrlPanel.Controls.Add(xLabel);
        ctrlPanel.Controls.Add(_xFieldCombo);

        // X粒度
        var granLabel = new Label { Text = "粒度:", AutoSize = true, Location = new Point(216, cy), Font = lblFont };
        _xGranBox = new NumericUpDown { Location = new Point(266, cy - 1), Width = 70, Minimum = 0, Maximum = 100000, Value = 0, Font = lblFont };
        _xGranBox.ValueChanged += (_, _) => ApplyXGranularity();
        var granHint = new Label { Text = "(0=自动)", AutoSize = true, Location = new Point(340, cy), Font = new Font("Microsoft YaHei", 8), ForeColor = Color.Gray };
        ctrlPanel.Controls.Add(granLabel);
        ctrlPanel.Controls.Add(_xGranBox);
        ctrlPanel.Controls.Add(granHint);

        // 背景色
        var bgLabel = new Label { Text = "背景:", AutoSize = true, Location = new Point(412, cy), Font = lblFont };
        var bgCombo = new ComboBox { Location = new Point(458, cy - 1), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, Font = lblFont };
        bgCombo.Items.AddRange(new object[] { "白色", "浅灰", "黑色", "深灰" });
        bgCombo.SelectedIndex = 0;
        bgCombo.SelectedIndexChanged += (_, _) => ApplyBackground(bgCombo.SelectedIndex);
        ctrlPanel.Controls.Add(bgLabel);
        ctrlPanel.Controls.Add(bgCombo);

        _rightPanel.Controls.Add(ctrlPanel);

        // 图表区域
        _plotModel = new PlotModel { TitleFontSize = 11, TitlePadding = 6 };
        _plotModel.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.BottomCenter,
            LegendPlacement = LegendPlacement.Outside,
            LegendOrientation = LegendOrientation.Horizontal,
            LegendMargin = 10,
            LegendPadding = 3,
            LegendFontSize = 9
        });
        _plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom, Title = "X",
            MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot
        });
        _plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left, Title = "Y",
            MajorGridlineStyle = LineStyle.Dash, MinorGridlineStyle = LineStyle.Dot
        });

        _plotView = new PlotView { Model = _plotModel, Dock = DockStyle.Fill };
        _plotView.AllowDrop = true;
        _plotView.DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.Text) == true) e.Effect = DragDropEffects.Copy; };
        _plotView.DragDrop += (_, e) =>
        {
            var text = e.Data?.GetData(DataFormats.Text) as string;
            if (text != null)
            {
                var parts = text.Split('\t');
                if (parts.Length == 2) PlotField(parts[0], parts[1]);
            }
        };

        // 双击删除最后一条曲线
        _plotView.MouseDoubleClick += (_, _) =>
        {
            if (_plotted.Count > 0)
            {
                var last = _plotted[^1];
                _plotModel.Series.Remove(last.series);
                _plotted.Remove(last);
                UpdateTitle();
                _plotModel.InvalidatePlot(true);
                _statusLabel.Text = $"已删除: {last.field}";
            }
        };

        // 单击显示数据
        _plotView.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || _plotted.Count == 0) return;
            ShowClickData(e.Location);
        };

        // 右键菜单：保存图片
        var ctxMenu = new ContextMenuStrip();
        ctxMenu.Items.Add("保存为图片...", null, (_, _) => SaveChartImage());
        _plotView.ContextMenuStrip = ctxMenu;

        _rightPanel.Controls.Add(_plotView);

        // ── 绑定Resize事件 ──
        Resize += (_, _) => DoLayout();

        // 初始布局
        Shown += (_, _) => DoLayout();
    }

    // ════════════════ 图表管理 ════════════════

    private void UpdateTitle()
    {
        string title = _plotted.Count > 0
            ? $"CurveAnalysis V13  ({_plotted.Count} 条曲线)"
            : "CurveAnalysis V13";
        Text = title;
    }

    private void ClearChart()
    {
        _plotModel.Series.Clear();
        _plotted.Clear();
        _colorIndex = 0;
        UpdateTitle();
        _plotModel.InvalidatePlot(true);
    }

    private void ResetView()
    {
        foreach (var axis in _plotModel.Axes) axis.Reset();
        _plotModel.InvalidatePlot(true);
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
        _fieldTree.Location = new Point(0, y); _fieldTree.Size = new Size(w, Math.Max(100, p.ClientSize.Height - y));
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

        // 图表铺满剩余空间
        _plotView.Location = new Point(0, y);
        _plotView.Size = new Size(w, p.ClientSize.Height - y);
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
        // 移除旧文件的曲线
        foreach (var p in _plotted.Where(p => p.file == old).ToList())
        {
            _plotModel.Series.Remove(p.series);
            _plotted.Remove(p);
        }
        var fm = new FileManager(d.FileName) { HeaderRow = dlg.CsvHeaderRow, DataStartRow = dlg.CsvDataStartRow };
        fm.Reload();
        _files[index] = fm;
        UpdateTitle();
        _plotModel.InvalidatePlot(true);
        RefreshUI();
    }

    private void RemoveFile(int index)
    {
        if (index < 0 || index >= _files.Count) return;
        var f = _files[index]; _files.RemoveAt(index);
        // 移除该文件的曲线
        foreach (var p in _plotted.Where(p => p.file == f).ToList())
        {
            _plotModel.Series.Remove(p.series);
            _plotted.Remove(p);
        }
        _selectedFileIndex = Math.Max(0, Math.Min(_selectedFileIndex, _files.Count - 1));
        UpdateTitle();
        _plotModel.InvalidatePlot(true);
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

    private void PlotField(string fp, string field)
    {
        var file = _files.FirstOrDefault(f => f.FilePath == fp);
        if (file == null) return;
        if (_plotted.Any(p => p.file == file && p.field == field)) return;

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

        var color = Palette[_colorIndex % Palette.Length];
        _colorIndex++;
        string legendText = string.IsNullOrEmpty(prefix) ? field : $"{prefix}: {field}";
        var series = new LineSeries { Title = legendText, Color = color, StrokeThickness = 2.5, MarkerType = MarkerType.None };
        for (int i = 0; i < xs.Length; i++)
        {
            if (!double.IsNaN(ys[i])) series.Points.Add(new DataPoint(xs[i], ys[i]));
        }

        _plotModel.Series.Add(series);
        _plotted.Add((file, field, series));
        UpdateTitle();
        _plotModel.InvalidatePlot(true);
        _statusLabel.Text = $"已绘制: {legendText} ({len} 点)";
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

    private void ReplotChart()
    {
        var plotted = _plotted.Select(p => (p.file.FilePath, p.field)).ToList();
        _plotModel.Series.Clear();
        _plotted.Clear();
        _colorIndex = 0;
        foreach (var (fp, field) in plotted) PlotField(fp, field);
    }

    private void ApplyXGranularity()
    {
        int gran = (int)_xGranBox.Value;
        foreach (var axis in _plotModel.Axes)
        {
            if (axis.Position == AxisPosition.Bottom && axis is LinearAxis la)
            {
                if (gran > 0)
                {
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
        _plotModel.InvalidatePlot(true);
    }

    // ════════════════ 单击显示数据 ════════════════

    private void ShowClickData(Point location)
    {
        var xAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
        if (xAxis == null) return;
        double dataX = xAxis.InverseTransform(location.X);

        // 清除旧的点击标签
        var oldLabels = _plotModel.Annotations
            .OfType<OxyPlot.Annotations.TextAnnotation>()
            .Where(a => a.Tag as string == "click_label")
            .ToList();
        foreach (var old in oldLabels) _plotModel.Annotations.Remove(old);

        // 收集交叉点
        var points = new List<(int idx, double xVal, double yVal)>();
        int fi = 1;
        foreach (var p in _plotted)
        {
            var yData = p.file.GetData(p.field);
            if (yData.Length == 0) { fi++; continue; }
            var xData = GetXData(p.file, yData.Length);
            if (xData.Length == 0) { fi++; continue; }
            int idx = FindNearestIndex(xData, dataX);
            if (idx >= 0 && idx < yData.Length && !double.IsNaN(yData[idx]))
                points.Add((fi, xData[idx], yData[idx]));
            fi++;
        }

        // 排序并错开重叠
        var yAxis = _plotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
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
            _plotModel.Annotations.Add(label);
        }

        _plotModel.InvalidatePlot(true);
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

    // ════════════════ 保存图片 ════════════════

    private void SaveChartImage()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png|所有文件|*.*",
            FileName = "CurveAnalysis_V13.png"
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var exporter = new PngExporter
            {
                Width = _plotView.ClientSize.Width * 2,
                Height = _plotView.ClientSize.Height * 2
            };
            using var fs = File.Create(dlg.FileName);
            exporter.Export(_plotModel, fs);
        }
    }

    // ════════════════ 背景色 ════════════════

    private void ApplyBackground(int theme)
    {
        OxyColor plotBg, plotGrid;
        switch (theme)
        {
            case 1:
                plotBg = OxyColors.LightGray; plotGrid = OxyColors.Gray; break;
            case 2:
                plotBg = OxyColors.Black; plotGrid = OxyColors.DarkGray; break;
            case 3:
                plotBg = OxyColors.DimGray; plotGrid = OxyColors.Gray; break;
            default:
                plotBg = OxyColors.White; plotGrid = OxyColors.LightGray; break;
        }
        _plotModel.Background = plotBg;
        foreach (var axis in _plotModel.Axes)
        {
            axis.MajorGridlineColor = plotGrid;
            axis.MinorGridlineColor = plotGrid;
        }
        _plotModel.TextColor = (theme >= 2) ? OxyColors.White : OxyColors.Black;
        _plotModel.InvalidatePlot(true);
    }

    // ════════════════ 搜索过滤 ════════════════

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
}
