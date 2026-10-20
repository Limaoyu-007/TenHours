using TenHours.Core;

namespace TenHours;

internal sealed class HistoryForm : Form
{
    internal HistoryForm(TimerData data)
    {
        Text = "十小时 · 历史记录";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(530, 360);
        MinimumSize = new Size(420, 240);
        BackColor = Color.FromArgb(25, 29, 36);
        ForeColor = Color.FromArgb(230, 234, 240);
        Font = new Font("Microsoft YaHei UI", 9);
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BackgroundColor = BackColor,
            BorderStyle = BorderStyle.None,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(48, 54, 64),
            VirtualMode = true
        };
        grid.DefaultCellStyle.BackColor = BackColor;
        grid.DefaultCellStyle.ForeColor = ForeColor;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 66, 83);
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(34, 40, 49);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 29;
        foreach (var title in new[] { "日期", "工作", "学习", "合计", "达标" }) grid.Columns.Add(title, title);
        grid.Columns[0].FillWeight = 135;
        grid.Columns[4].FillWeight = 60;
        var rows = data.Days.Reverse().Select(x => (x.Key, x.Value.WorkTicks, x.Value.StudyTicks, x.Value.TotalTicks)).ToArray();
        grid.CellValueNeeded += (_, e) =>
        {
            var row = rows[e.RowIndex];
            e.Value = e.ColumnIndex switch
            {
                0 => row.Key,
                1 => Format(row.WorkTicks),
                2 => Format(row.StudyTicks),
                3 => Format(row.TotalTicks),
                _ => row.TotalTicks >= TimerEngine.GoalTicks ? "✓" : "—"
            };
        };
        grid.RowCount = rows.Length;
        var footer = new Label
        {
            Dock = DockStyle.Bottom, Height = 34, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0), ForeColor = Color.FromArgb(155, 165, 180),
            Text = "时长为 时:分:秒 · 历史仅保存在此电脑 · 本窗口为打开时的快照"
        };
        Controls.Add(grid);
        Controls.Add(footer);
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    internal static string Format(long ticks)
    {
        long seconds = ticks / TimeSpan.TicksPerSecond;
        return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }
}
