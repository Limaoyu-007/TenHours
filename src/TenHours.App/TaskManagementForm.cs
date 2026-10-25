using TenHours.Core;

namespace TenHours;

internal sealed class TaskManagementForm : Form
{
    private readonly TimerData data;
    private readonly DataGridView grid = new();
    private readonly TextBox title = new() { MaxLength = 80, Dock = DockStyle.Fill };
    private readonly CheckBox daily = new() { Text = "每日任务 · 每天重置勾选", AutoSize = true };
    private readonly Button save;
    private readonly Button remove;
    private readonly Button up;
    private readonly Button down;
    private bool loading;

    private Guid? SelectedId => grid.SelectedRows.Count == 0 ? null : (Guid)grid.SelectedRows[0].Tag!;

    internal TaskManagementForm(TimerData data, Func<string, bool, Guid> add,
        Action<Guid, string, bool> update, Action<Guid> delete, Action<Guid, int> move)
    {
        this.data = data;
        Text = "十小时 · 管理任务";
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 9);
        ClientSize = new Size(570, 510);
        MinimumSize = new Size(510, 440);
        BackColor = Color.FromArgb(24, 29, 38);
        ForeColor = Color.FromArgb(218, 228, 239);
        Padding = new Padding(18);

        var intro = new Label { Dock = DockStyle.Top, Height = 46,
            Text = "管理任务\n选择任务后修改名称或每日设置，点击「保存修改」生效。", AutoSize = false };
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = grid.AllowUserToDeleteRows = grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = BackColor;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = Color.FromArgb(44, 53, 66);
        grid.DefaultCellStyle.BackColor = Color.FromArgb(29, 36, 46);
        grid.DefaultCellStyle.ForeColor = ForeColor;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(48, 68, 88);
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 43, 55);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
        grid.ColumnHeadersHeight = 32;
        grid.RowTemplate.Height = 30;
        grid.Columns.Add("title", "任务名称");
        grid.Columns.Add("repeat", "类型");
        grid.Columns.Add("status", "状态");
        grid.Columns[0].FillWeight = 260;
        grid.Columns[1].FillWeight = 85;
        grid.Columns[2].FillWeight = 85;
        foreach (DataGridViewColumn column in grid.Columns) column.SortMode = DataGridViewColumnSortMode.NotSortable;
        grid.SelectionChanged += (_, _) => { if (!loading) LoadSelection(); };

        var editor = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 102, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(0, 12, 0, 0) };
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        editor.Controls.Add(new Label { Text = "任务名称", AutoSize = true }, 0, 0);
        title.BackColor = Color.FromArgb(35, 43, 55);
        title.ForeColor = ForeColor;
        title.BorderStyle = BorderStyle.FixedSingle;
        editor.Controls.Add(title, 0, 1);
        editor.Controls.Add(daily, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 39, WrapContents = false,
            Padding = new Padding(0, 6, 0, 0) };
        Button ActionButton(string text, EventHandler action)
        {
            var button = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 50, 65), ForeColor = ForeColor, Cursor = Cursors.Hand };
            button.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 97);
            button.Click += action;
            actions.Controls.Add(button);
            return button;
        }
        ActionButton("添加任务", (_, _) =>
        {
            using var dialog = new TaskNameForm();
            if (dialog.ShowDialog(this) == DialogResult.OK) add(dialog.TaskTitle, dialog.IsDaily);
        });
        save = ActionButton("保存修改", (_, _) =>
        {
            if (SelectedId is not Guid id || string.IsNullOrWhiteSpace(title.Text)) return;
            update(id, title.Text, daily.Checked);
        });
        remove = ActionButton("删除", (_, _) => { if (SelectedId is Guid id) delete(id); });
        up = ActionButton("上移", (_, _) => { if (SelectedId is Guid id) move(id, -1); });
        down = ActionButton("下移", (_, _) => { if (SelectedId is Guid id) move(id, 1); });
        title.TextChanged += (_, _) => save.Enabled = SelectedId is not null && !string.IsNullOrWhiteSpace(title.Text);
        var hint = new Label { Dock = DockStyle.Bottom, Height = 34, ForeColor = Color.FromArgb(150, 168, 187),
            Text = "普通任务跨天保留完成状态。每日任务在新的一天恢复为未完成。", TextAlign = ContentAlignment.BottomLeft };
        Controls.Add(grid);
        Controls.Add(intro);
        Controls.Add(editor);
        Controls.Add(actions);
        Controls.Add(hint);
        Reload();
    }

    internal void Reload(Guid? select = null)
    {
        select ??= SelectedId;
        loading = true;
        grid.Rows.Clear();
        foreach (var task in data.Tasks!)
        {
            int index = grid.Rows.Add(task.Title, task.IsDaily ? "每日任务" : "普通任务", task.IsCompleted ? "已完成" : "未完成");
            grid.Rows[index].Tag = task.Id;
        }
        grid.ClearSelection();
        var selected = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(row => (Guid)row.Tag! == select)
            ?? grid.Rows.Cast<DataGridViewRow>().FirstOrDefault();
        if (selected is not null) { grid.CurrentCell = selected.Cells[0]; selected.Selected = true; }
        loading = false;
        LoadSelection();
    }

    internal void RefreshStatuses()
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            var task = data.Tasks!.FirstOrDefault(task => task.Id == (Guid)row.Tag!);
            if (task is not null) row.Cells[2].Value = task.IsCompleted ? "已完成" : "未完成";
        }
    }

    private void LoadSelection()
    {
        var task = data.Tasks!.FirstOrDefault(task => task.Id == SelectedId);
        title.Enabled = daily.Enabled = remove.Enabled = task is not null;
        title.Text = task?.Title ?? "";
        daily.Checked = task?.IsDaily ?? false;
        save.Enabled = task is not null;
        int index = task is null ? -1 : data.Tasks!.IndexOf(task);
        up.Enabled = index > 0;
        down.Enabled = index >= 0 && index < data.Tasks!.Count - 1;
    }
}
