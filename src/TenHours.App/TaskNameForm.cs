namespace TenHours;

internal sealed class TaskNameForm : Form
{
    private readonly TextBox input = new() { MaxLength = 80, Dock = DockStyle.Fill, PlaceholderText = "写下一件想完成的事" };
    private string? acceptedTitle;
    private bool acceptedDaily;
    private readonly CheckBox daily = new() { Text = "每日任务（每天重新完成）", Dock = DockStyle.Bottom, Height = 30, AutoSize = false };
    internal string TaskTitle => acceptedTitle ?? input.Text.Trim();
    internal bool IsDaily => acceptedTitle is null ? daily.Checked : acceptedDaily;

    internal TaskNameForm(string? current = null, bool isDaily = false)
    {
        Text = current is null ? "添加任务" : "编辑任务";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        TopMost = true;
        MaximizeBox = MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(312, 172);
        Padding = new Padding(16);
        BackColor = Color.FromArgb(24, 29, 38);
        ForeColor = Color.FromArgb(218, 228, 239);
        input.Text = current ?? "";
        daily.Checked = isDaily;
        var label = new Label { Text = "任务名称", Dock = DockStyle.Top, Height = 22 };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 31, FlowDirection = FlowDirection.RightToLeft };
        var confirm = new Button { Text = "确定", AutoSize = true, Enabled = TaskTitle.Length > 0 };
        var cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
        input.TextChanged += (_, _) => confirm.Enabled = TaskTitle.Length > 0;
        confirm.Click += (_, _) => { acceptedTitle = input.Text.Trim(); acceptedDaily = daily.Checked; DialogResult = DialogResult.OK; Close(); };
        actions.Controls.AddRange([confirm, cancel]);
        Controls.Add(input);
        Controls.Add(label);
        Controls.Add(daily);
        Controls.Add(actions);
        AcceptButton = confirm;
        CancelButton = cancel;
        Shown += (_, _) => { input.Focus(); input.SelectAll(); };
    }
}
