using TenHours.Core;

namespace TenHours;

internal sealed class TimeDeductionForm : Form
{
    private readonly DateTimePicker date = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd", Width = 220 };
    private readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly NumericUpDown hours = new() { Minimum = 0, Maximum = 999999, Width = 95 };
    private readonly NumericUpDown minutes = new() { Minimum = 0, Maximum = 59, Width = 60 };
    private readonly CheckBox clear = new() { Text = "清零所选日期的这一类别", AutoSize = true };
    private readonly Label preview = new() { AutoSize = true, MaximumSize = new Size(350, 0) };
    private readonly Label error = new() { AutoSize = true, MaximumSize = new Size(350, 0), ForeColor = Color.FromArgb(255, 120, 130) };
    private readonly Button confirm = new() { Text = "确认扣减", AutoSize = true };
    private readonly TimerData data;
    private readonly Action<DateTime, ActivityMode, TimeSpan, bool> apply;

    internal TimeDeductionForm(TimerData data, DateTime today, Action<DateTime, ActivityMode, TimeSpan, bool> apply)
    {
        this.data = data;
        this.apply = apply;
        Text = "十小时 · 扣减 / 清零时间";
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
        date.MaxDate = today;
        date.Value = today;
        mode.Items.AddRange(["工作", "学习"]);
        mode.SelectedIndex = data.Mode == ActivityMode.Work ? 0 : 1;

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
            control.Margin = new Padding(0, 4, 0, 10);
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }
        FullRow(new Label { Text = "计时已暂停，处理完成后请手动继续。", AutoSize = true, ForeColor = Color.FromArgb(159, 170, 185) });
        Row("日期", date);
        Row("扣减类别", mode);
        var duration = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        duration.Controls.Add(hours);
        duration.Controls.Add(new Label { Text = "小时", AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        duration.Controls.Add(minutes);
        duration.Controls.Add(new Label { Text = "分钟", AutoSize = true, Margin = new Padding(0, 6, 0, 0) });
        Row("多记时长", duration);
        FullRow(clear);
        FullRow(preview);
        FullRow(new Label { Text = "只调整所选类别；另一类别和任务记录保留。", AutoSize = true, ForeColor = Color.FromArgb(159, 170, 185) });
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
        mode.SelectedIndexChanged += (_, _) => UpdatePreview();
        hours.ValueChanged += (_, _) => UpdatePreview();
        minutes.ValueChanged += (_, _) => UpdatePreview();
        clear.CheckedChanged += (_, _) => UpdatePreview();
        confirm.Click += (_, _) => Submit();
        UpdatePreview();
    }

    private ActivityMode SelectedMode => mode.SelectedIndex == 0 ? ActivityMode.Work : ActivityMode.Study;
    private TimeSpan Duration => TimeSpan.FromTicks((long)hours.Value * TimeSpan.TicksPerHour + (long)minutes.Value * TimeSpan.TicksPerMinute);
    private long Available
    {
        get
        {
            string key = date.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (!data.Days.TryGetValue(key, out var day)) return 0;
            return SelectedMode == ActivityMode.Work ? day.WorkTicks : day.StudyTicks;
        }
    }
    private static string Format(long ticks)
    {
        long seconds = ticks / TimeSpan.TicksPerSecond;
        return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }
    private void UpdatePreview()
    {
        hours.Enabled = minutes.Enabled = !clear.Checked;
        long available = Available;
        long deduction = clear.Checked ? available : Duration.Ticks;
        bool valid = deduction > 0 && deduction <= available;
        preview.Text = $"{mode.SelectedItem}当前累计：{Format(available)}\n" +
            (valid ? $"本次扣减：{Format(deduction)} → 剩余：{Format(available - deduction)}" : "请输入多记的时长，或勾选清零这一类别。");
        error.Text = deduction > available ? "扣减时长不能超过所选类别的累计时间。" : "";
        confirm.Text = clear.Checked ? "确认清零" : "确认扣减";
        confirm.Enabled = valid;
    }
    private void Submit()
    {
        confirm.Enabled = false;
        try
        {
            apply(date.Value.Date, SelectedMode, Duration, clear.Checked);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            error.Text = ex.Message;
            confirm.Enabled = true;
        }
    }
}
