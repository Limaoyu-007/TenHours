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
        ClientSize = new Size(650, 520);
        MinimumSize = new Size(540, 420);
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
        foreach (var title in new[] { "日期", "工作时长", "学习时长", "总计", "达标", "完成任务" }) grid.Columns.Add(title, title);
        foreach (DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
        grid.Columns[0].FillWeight = 135;
        grid.Columns[4].FillWeight = 60;
        grid.Columns[1].DefaultCellStyle.ForeColor = Color.FromArgb(108, 184, 248);
        grid.Columns[2].DefaultCellStyle.ForeColor = Color.FromArgb(188, 158, 250);
        var rows = data.Days.Reverse().Select(x => (x.Key, x.Value.WorkTicks, x.Value.StudyTicks, x.Value.TotalTicks,
            x.Value.TaskHistoryRecorded, Tasks: x.Value.CompletedTasks.Select(task => (task.Title, task.IsDaily)).ToArray())).ToArray();
        grid.CellValueNeeded += (_, e) =>
        {
            var row = rows[e.RowIndex];
            e.Value = e.ColumnIndex switch
            {
                0 => row.Key,
                1 => Format(row.WorkTicks),
                2 => Format(row.StudyTicks),
                3 => Format(row.TotalTicks),
                4 => row.TotalTicks >= TimerEngine.GoalTicks ? "✓" : "—",
                _ => row.TaskHistoryRecorded ? $"{row.Tasks.Length} 项" : "未记录"
            };
        };
        grid.RowCount = rows.Length;
        var details = new RichTextBox
        {
            Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
            BackColor = BackColor, ForeColor = ForeColor, DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical, Text = "选择日期，查看当天完成的任务。"
        };
        var detailHeading = new Label
        {
            Dock = DockStyle.Top, Height = 30, Text = "当天完成的任务", TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(155, 190, 216)
        };
        var taskDetails = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 4) };
        taskDetails.Controls.Add(details);
        taskDetails.Controls.Add(detailHeading);
        void ShowSelectedTasks()
        {
            int index = grid.CurrentCell?.RowIndex ?? -1;
            if (index < 0 || index >= rows.Length) return;
            var row = rows[index];
            detailHeading.Text = row.Key + " · 完成任务";
            details.Text = !row.TaskHistoryRecorded ? "该日期尚未启用任务记录，无法还原当时完成的任务。"
                : row.Tasks.Length == 0 ? "当天没有记录到已完成任务。"
                : string.Join(Environment.NewLine, row.Tasks.Select(task => "✓  " + task.Title + (task.IsDaily ? "  · 每日任务" : "")));
            details.SelectionStart = 0;
            details.ScrollToCaret();
        }
        grid.SelectionChanged += (_, _) => ShowSelectedTasks();
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        content.Controls.Add(grid, 0, 0);
        content.Controls.Add(taskDetails, 0, 1);
        var footer = new Label
        {
            Dock = DockStyle.Bottom, Height = 48, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0), ForeColor = Color.FromArgb(155, 165, 180),
            Text = "时长为 时:分:秒 · 本窗口为打开时的快照\n任务记录从 1.4.0 开始；升级前的完成日期无法补回。"
        };
        Controls.Add(content);
        Controls.Add(footer);
        Shown += (_, _) => ShowSelectedTasks();
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    internal static string Format(long ticks)
    {
        long seconds = ticks / TimeSpan.TicksPerSecond;
        return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }
}
