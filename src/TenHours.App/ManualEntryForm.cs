using TenHours.Core;

namespace TenHours;

internal sealed class ManualEntryForm : Form
{
    private readonly DateTimePicker date = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Width = 220 };
    private readonly DateTimePicker start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 220 };
    private readonly DateTimePicker end = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 220 };
    private readonly CheckBox nextDay = new() { Text = "结束时间在次日", AutoSize = true };
    private readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Label preview = new() { AutoSize = true, ForeColor = Color.FromArgb(95, 222, 164), Padding = new Padding(0, 5, 0, 5) };
    private readonly Label error = new() { AutoSize = true, MaximumSize = new Size(335, 0), ForeColor = Color.FromArgb(255, 120, 130) };
    private readonly Button confirm = new() { Text = "确认补录", AutoSize = true, Name = "ConfirmManualEntry" };
    private readonly Action<DateTime, DateTime, ActivityMode> apply;

    internal ManualEntryForm(DateTime now, ActivityMode currentMode, Action<DateTime, DateTime, ActivityMode> apply)
    {
        this.apply = apply;
        Text = "十小时 · 补录时间";
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Microsoft YaHei UI", 9);
        BackColor = Color.FromArgb(25, 29, 36);
        ForeColor = Color.FromArgb(230, 234, 240);
        Padding = new Padding(18);
        date.Value = now.Date;
        date.MaxDate = now.Date;
        end.Value = now.Date.AddHours(now.Hour).AddMinutes(now.Minute);
        start.Value = end.Value.AddHours(-1);
        // At 00:xx the previous hour belongs to yesterday.
        date.Value = start.Value.Date;
        nextDay.Checked = start.Value.Date != end.Value.Date;
        mode.Items.AddRange(["工作", "学习"]);
        mode.SelectedIndex = currentMode == ActivityMode.Work ? 0 : 1;
        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        void Row(string label, Control control)
        {
            int row = layout.RowCount++;
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 7, 12, 10) }, 0, row);
            control.Margin = new Padding(0, 3, 0, 9);
            layout.Controls.Add(control, 1, row);
        }
        void FullRow(Control control)
        {
            int row = layout.RowCount++;
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }
        Row("日期", date);
        Row("开始时间", start);
        Row("结束时间", end);
        Row("", nextDay);
        Row("计入类型", mode);
        FullRow(preview);
        FullRow(new Label
        {
            Text = "直接加到已有累计；与已有时段重叠也会累加。",
            AutoSize = true, ForeColor = Color.FromArgb(159, 170, 185), Margin = new Padding(0, 4, 0, 10)
        });
        FullRow(error);
        var actions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, AutoSize = true };
        actions.Controls.Add(confirm);
        actions.Controls.Add(cancel);
        FullRow(actions);
        Controls.Add(layout);
        AcceptButton = confirm;
        CancelButton = cancel;
        date.ValueChanged += (_, _) => UpdatePreview();
        start.ValueChanged += (_, _) => UpdatePreview();
        end.ValueChanged += (_, _) => UpdatePreview();
        nextDay.CheckedChanged += (_, _) => UpdatePreview();
        mode.SelectedIndexChanged += (_, _) => UpdatePreview();
        confirm.Click += (_, _) => Submit();
        UpdatePreview();
    }

    private DateTime StartTime => date.Value.Date + start.Value.TimeOfDay;
    private DateTime EndTime => date.Value.Date.AddDays(nextDay.Checked ? 1 : 0) + end.Value.TimeOfDay;

    private void UpdatePreview()
    {
        var duration = EndTime - StartTime;
        preview.Text = duration > TimeSpan.Zero ? $"本次计入{mode.SelectedItem}：{(int)duration.TotalHours} 小时 {duration.Minutes:00} 分钟" : "请选择有效的开始和结束时间";
        error.Text = "";
    }

    private void Submit()
    {
        confirm.Enabled = false;
        try
        {
            apply(StartTime, EndTime, mode.SelectedIndex == 0 ? ActivityMode.Work : ActivityMode.Study);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            error.Text = ex.Message;
            confirm.Enabled = true;
        }
    }

    // The integration probe drives the same controls and confirmation handler as users.
    internal void SetInput(DateTime from, DateTime until, ActivityMode activity)
    {
        date.Value = from.Date;
        start.Value = from;
        end.Value = until;
        nextDay.Checked = until.Date > from.Date;
        mode.SelectedIndex = activity == ActivityMode.Work ? 0 : 1;
    }
    internal void ConfirmInput() => confirm.PerformClick();
    internal string PreviewText => preview.Text;
}
