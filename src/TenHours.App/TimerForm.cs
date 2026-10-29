using System.Drawing.Drawing2D;
using TenHours.Core;

namespace TenHours;

internal sealed class TimerForm : Form
{
    private static readonly Color Background = Color.FromArgb(20, 25, 33);
    private static readonly Color Muted = Color.FromArgb(159, 170, 185);
    private static readonly Color Red = Color.FromArgb(255, 99, 111);
    private static readonly Color Green = Color.FromArgb(82, 215, 155);
    private static readonly Color WorkAccent = Color.FromArgb(108, 184, 248);
    private static readonly Color StudyAccent = Color.FromArgb(188, 158, 250);
    private const int ModeSectionHeight = 60;
    private readonly TimerEngine engine;
    private readonly JsonStore store;
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly NotifyIcon tray;
    private readonly Icon redIcon;
    private readonly Icon greenIcon;
    private readonly Button toggle = new();
    private readonly Button workButton = new();
    private readonly Button studyButton = new();
    private readonly Button addTask = new();
    private readonly Button collapseTasks = new();
    private readonly Button taskOptions = new();
    private readonly ContextMenuStrip taskDisplayMenu = new();
    private readonly ToolStripMenuItem hideCompletedItem = new("隐藏已完成");
    private readonly ToolStripMenuItem dailyFirstItem = new("每日任务置顶");
    private readonly ToolStripMenuItem collapseItem = new("折叠任务区");
    private readonly TaskListPanel taskPanel = new() { BackColor = Color.FromArgb(24, 30, 40) };
    private readonly ToolTip tooltip = new();
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem startItem;
    private readonly ToolStripMenuItem pauseItem;
    private readonly ToolStripMenuItem workItem;
    private readonly ToolStripMenuItem studyItem;
    private readonly ToolStripMenuItem startupItem;
    private readonly ToolStripMenuItem retryItem;
    private Font timeFont = null!;
    private Font detailFont = null!;
    private HistoryForm? history;
    private TaskManagementForm? taskManager;
    private string? lastPresentation;
    private string? saveError;
    private int runningTicks;
    private bool initialized;
    private bool exiting;
    private bool sessionRegistered;
    private DateTime lastDate;
    private readonly string? recoveryWarning;

    internal TimerForm(TimerEngine engine, JsonStore store, string? warning = null)
    {
        this.engine = engine;
        this.store = store;
        bool initializedTasks = TaskDefaults.Initialize(engine.Data);
        initializedTasks |= engine.Data.AdvanceChecklistDate(engine.Today);
        lastDate = engine.Today;
        recoveryWarning = warning;
        Text = "十小时 · TenHours";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Background;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);

        redIcon = CreateIcon(Red);
        greenIcon = CreateIcon(Green);
        Icon = redIcon;
        tray = new NotifyIcon { Icon = redIcon, Text = "十小时 · 已暂停", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => Reveal();
        menu.Items.Add("显示悬浮窗", null, (_, _) => Reveal());
        menu.Items.Add("隐藏到托盘", null, (_, _) => Hide());
        menu.Items.Add(new ToolStripSeparator());
        startItem = new ToolStripMenuItem("开始", null, (_, _) => StartTiming());
        pauseItem = new ToolStripMenuItem("暂停", null, (_, _) => PauseTiming());
        workItem = new ToolStripMenuItem("工作", null, (_, _) => Switch(ActivityMode.Work));
        studyItem = new ToolStripMenuItem("学习", null, (_, _) => Switch(ActivityMode.Study));
        menu.Items.AddRange([startItem, pauseItem, new ToolStripSeparator(), workItem, studyItem]);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("补录时间…", null, (_, _) => ShowManualEntry());
        menu.Items.Add("扣减 / 清零时间…", null, (_, _) => ShowTimeDeduction());
        menu.Items.Add("查看历史", null, (_, _) => ShowHistory());
        menu.Items.Add("管理任务…", null, (_, _) => ShowTaskManager());
        var display = new ToolStripMenuItem("清单显示") { DropDown = taskDisplayMenu };
        taskDisplayMenu.Items.AddRange([hideCompletedItem, dailyFirstItem, collapseItem]);
        hideCompletedItem.Click += (_, _) => ChangeTaskDisplay(() => engine.Data.HideCompletedTasks = !engine.Data.HideCompletedTasks);
        dailyFirstItem.Click += (_, _) => ChangeTaskDisplay(() => engine.Data.DailyTasksFirst = !engine.Data.DailyTasksFirst);
        collapseItem.Click += (_, _) => ToggleTaskCollapse();
        taskDisplayMenu.Opening += (_, _) =>
        {
            hideCompletedItem.Checked = engine.Data.HideCompletedTasks;
            dailyFirstItem.Checked = engine.Data.DailyTasksFirst;
            collapseItem.Checked = engine.Data.TasksCollapsed;
        };
        menu.Items.Add(display);
        var settings = new ToolStripMenuItem("设置");
        startupItem = new ToolStripMenuItem("开机自启（当前用户）", null, (_, _) => ToggleStartup());
        settings.DropDownItems.Add(startupItem);
        settings.DropDownItems.Add("打开数据目录", null, (_, _) => OpenDataDirectory());
        menu.Items.Add(settings);
        retryItem = new ToolStripMenuItem("重试保存", null, (_, _) => { TrySave(); RefreshState(); });
        menu.Items.Add(retryItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApplication());
        menu.Opening += (_, _) =>
        {
            RefreshState();
            try { startupItem.Checked = StartupRegistration.Enabled; }
            catch (System.Security.SecurityException) { startupItem.Enabled = false; }
        };
        ContextMenuStrip = menu;

        toggle.FlatStyle = FlatStyle.Flat;
        toggle.FlatAppearance.BorderSize = 0;
        toggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(67, 78, 93);
        toggle.FlatAppearance.MouseDownBackColor = Color.FromArgb(77, 89, 105);
        toggle.BackColor = Color.FromArgb(45, 54, 67);
        toggle.ForeColor = Color.FromArgb(233, 238, 245);
        toggle.Cursor = Cursors.Hand;
        toggle.AccessibleName = "开始或暂停计时";
        toggle.Name = "ToggleTimer";
        toggle.Click += (_, _) => { if (engine.IsRunning) PauseTiming(); else StartTiming(); };
        Controls.Add(toggle);
        foreach (var button in new[] { workButton, studyButton })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(button);
        }
        workButton.Name = "SelectWork";
        studyButton.Name = "SelectStudy";
        workButton.Click += (_, _) => Switch(ActivityMode.Work);
        studyButton.Click += (_, _) => Switch(ActivityMode.Study);
        tooltip.SetToolTip(workButton, "工作：面向实际问题与具体交付。\n点击切换；计时中会结算上一类并继续，暂停时仍保持暂停。");
        tooltip.SetToolTip(studyButton, "学习：积累知识、练习技能与研究方法。\n点击切换；计时中会结算上一类并继续，暂停时仍保持暂停。");
        addTask.Text = "管理任务";
        addTask.FlatStyle = FlatStyle.Flat;
        addTask.FlatAppearance.BorderColor = Color.FromArgb(45, 56, 72);
        addTask.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 42, 54);
        addTask.ForeColor = Color.FromArgb(155, 175, 196);
        addTask.Cursor = Cursors.Hand;
        addTask.Click += (_, _) => ShowTaskManager();
        Controls.Add(taskPanel);
        Controls.Add(addTask);
        foreach (var button in new[] { collapseTasks, taskOptions })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 42, 54);
            button.ForeColor = Color.FromArgb(195, 208, 224);
            button.Cursor = Cursors.Hand;
            Controls.Add(button);
        }
        collapseTasks.TextAlign = ContentAlignment.MiddleLeft;
        collapseTasks.AccessibleName = "展开或折叠任务区";
        collapseTasks.Click += (_, _) => ToggleTaskCollapse();
        taskOptions.Text = "···";
        taskOptions.AccessibleName = "清单显示选项";
        taskOptions.Click += (_, _) => taskDisplayMenu.Show(taskOptions, new Point(0, taskOptions.Height));
        tooltip.SetToolTip(taskOptions, "隐藏已完成 · 每日任务置顶 · 折叠任务区");
        tooltip.SetToolTip(this, "点击工作 / 学习切换 · 拖动背景移动 · 双击托盘图标恢复");
        timer.Tick += (_, _) => OnTick();
        MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, 2, 0);
        };
        // WinForms applies its suggested bounds after raising DpiChanged. Relayout
        // on the next message so the new DPI and the final bounds are both available.
        DpiChanged += (_, _) => BeginInvoke((Action)(() =>
        {
            if (!IsDisposed) { LayoutWidget(); ClampToScreen(); }
        }));
        Shown += (_, _) =>
        {
            LayoutWidget();
            var position = engine.Data.Position;
            Location = position is null ? new Point(Screen.PrimaryScreen!.WorkingArea.Right - Width - 24,
                Screen.PrimaryScreen.WorkingArea.Top + 48) : new Point(position.X, position.Y);
            ClampToScreen();
            initialized = true;
            if (initializedTasks) TrySave();
            RefreshState();
            ArmTimer();
            if (recoveryWarning is not null) tray.ShowBalloonTip(8000, "十小时 · 已恢复备份", recoveryWarning, ToolTipIcon.Warning);
        };
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x80; return cp; } // WS_EX_TOOLWINDOW
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        sessionRegistered = NativeMethods.WTSRegisterSessionNotification(Handle, 0);
        if (!sessionRegistered) throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error(), "无法注册锁屏通知；计时器无法安全运行。");
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (sessionRegistered) NativeMethods.WTSUnRegisterSessionNotification(Handle);
        sessionRegistered = false;
        base.OnHandleDestroyed(e);
    }

    // Control.DeviceDpi can fall back to system DPI when called from a hidden
    // timer/tray window's thread context. Geometry belongs to this HWND instead.
    internal int WindowDpi
    {
        get
        {
            uint dpi = IsHandleCreated ? NativeMethods.GetDpiForWindow(Handle) : 0;
            return dpi > 0 ? (int)dpi : DeviceDpi;
        }
    }

    private int Px(float value) => (int)Math.Round(value * WindowDpi / 96f);

    internal const int LogicalWidth = 260;
    internal int LogicalHeight => ModeSectionHeight + (engine.Data.TasksCollapsed ? 176 : 220 + 28 * Math.Clamp(engine.Data.VisibleTasks().Count, 1, 6));
    internal IReadOnlyList<CheckBox> TaskRows => taskPanel.Controls.OfType<CheckBox>().ToArray();

    private void LayoutWidget()
    {
        ClientSize = new Size(Px(LogicalWidth), Px(LogicalHeight));
        var previousTimeFont = timeFont;
        var previousDetailFont = detailFont;
        timeFont = new Font("Segoe UI", 32 * WindowDpi / 96f, FontStyle.Regular, GraphicsUnit.Pixel);
        detailFont = new Font("Microsoft YaHei UI", 11 * WindowDpi / 96f, FontStyle.Regular, GraphicsUnit.Pixel);
        toggle.Font = detailFont;
        toggle.SetBounds(Px(164), Px(95 + ModeSectionHeight), Px(80), Px(25));
        workButton.Font = studyButton.Font = detailFont;
        workButton.SetBounds(Px(16), Px(78), Px(110), Px(52));
        studyButton.SetBounds(Px(134), Px(78), Px(110), Px(52));
        UpdateModeButtons();
        collapseTasks.Font = taskOptions.Font = detailFont;
        collapseTasks.Text = (engine.Data.TasksCollapsed ? "▸  " : "▾  ") + "任务清单";
        collapseTasks.SetBounds(Px(12), Px(140 + ModeSectionHeight), Px(128), Px(25));
        taskOptions.SetBounds(Px(149), Px(140 + ModeSectionHeight), Px(27), Px(25));
        taskPanel.Visible = addTask.Visible = !engine.Data.TasksCollapsed;
        int panelHeight = Px(28 * Math.Clamp(engine.Data.VisibleTasks().Count, 1, 6));
        taskPanel.SetBounds(Px(12), Px(170 + ModeSectionHeight), Px(236), panelHeight);
        addTask.Font = detailFont;
        addTask.SetBounds(Px(16), Px(170 + ModeSectionHeight) + panelHeight + Px(10), Px(228), Px(26));
        RenderTasks();
        previousTimeFont?.Dispose();
        previousDetailFont?.Dispose();
        nint regionHandle = NativeMethods.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, Px(20), Px(20));
        using var sourceRegion = Region.FromHrgn(regionHandle);
        NativeMethods.DeleteObject(regionHandle);
        var old = Region;
        Region = sourceRegion.Clone();
        old?.Dispose();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (timeFont is null) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        long ticks = engine.TodayRecord.TotalTicks;
        bool reached = ticks >= TimerEngine.GoalTicks;
        Color signal = reached ? Green : Red;
        using var glow = new SolidBrush(Color.FromArgb(28, signal));
        using var lamp = new SolidBrush(signal);
        g.FillEllipse(glow, Px(220), Px(8), Px(24), Px(24));
        g.FillEllipse(lamp, Px(226), Px(14), Px(12), Px(12));
        TextRenderer.DrawText(g, "今日总计", detailFont, new Rectangle(Px(16), Px(11), Px(90), Px(18)), Muted,
            TextFormatFlags.Left | TextFormatFlags.NoPadding);
        string activeMode = engine.Data.Mode == ActivityMode.Work ? "工作" : "学习";
        TextRenderer.DrawText(g, activeMode + (engine.IsRunning ? " · 计时中" : " · 已暂停"), detailFont,
            new Rectangle(Px(108), Px(11), Px(106), Px(18)),
            engine.IsRunning ? (engine.Data.Mode == ActivityMode.Work ? WorkAccent : StudyAccent) : Muted,
            TextFormatFlags.Right | TextFormatFlags.NoPadding);
        string total = HoursMinutes(ticks, false);
        TextRenderer.DrawText(g, total, timeFont, new Rectangle(Px(16), Px(31), Px(220), Px(40)),
            Color.FromArgb(242, 245, 249), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        using var track = new SolidBrush(Color.FromArgb(47, 55, 67));
        var bar = new RectangleF(Px(16), Px(80 + ModeSectionHeight), Px(228), Px(4));
        g.FillRoundedRectangle(track, bar, new SizeF(Px(2), Px(2)));
        float progress = (float)Math.Clamp(ticks / (double)TimerEngine.GoalTicks, 0, 1);
        if (progress > 0) g.FillRoundedRectangle(lamp, new RectangleF(bar.X, bar.Y, Math.Max(Px(4), bar.Width * progress), bar.Height), new SizeF(Px(2), Px(2)));
        string remaining = saveError is not null ? "保存失败 · 已暂停" : reached ? "今日已达标" : "还需 " + HoursMinutes(TimerEngine.GoalTicks - ticks, true);
        TextRenderer.DrawText(g, remaining, detailFont, new Rectangle(Px(16), Px(95 + ModeSectionHeight), Px(140), Px(25)),
            saveError is not null ? Red : reached ? Green : Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        using var divider = new Pen(Color.FromArgb(40, 49, 63), 1);
        g.DrawLine(divider, Px(16), Px(133 + ModeSectionHeight), Px(244), Px(133 + ModeSectionHeight));
        int completed = engine.Data.Tasks!.Count(x => x.IsCompleted);
        TextRenderer.DrawText(g, $"{completed} / {engine.Data.Tasks!.Count}", detailFont,
            new Rectangle(Px(181), Px(145 + ModeSectionHeight), Px(63), Px(18)), Muted, TextFormatFlags.Right | TextFormatFlags.NoPadding);
        using var border = new Pen(Color.FromArgb(62, 71, 84), 1);
        using var outline = new GraphicsPath();
        outline.AddRoundedRectangle(new RectangleF(.5f, .5f, Width - 1, Height - 1), new SizeF(Px(10), Px(10)));
        g.DrawPath(border, outline);
    }

    private void RenderTasks()
    {
        var visible = engine.Data.VisibleTasks();
        int scroll = taskPanel.ScrollOffset;
        taskPanel.SuspendLayout();
        foreach (Control row in taskPanel.Controls.Cast<Control>().ToArray()) row.Dispose();
        taskPanel.Controls.Clear();
        int width = taskPanel.Width - Px(12);
        if (visible.Count == 0)
        {
            taskPanel.Controls.Add(new Label { Text = engine.Data.Tasks!.Count == 0 ? "写下一件想完成的事" : "已隐藏全部完成项", ForeColor = Muted,
                Font = detailFont, AutoSize = false, Bounds = new Rectangle(Px(7), 0, width - Px(7), Px(28)), TextAlign = ContentAlignment.MiddleLeft });
        }
        for (int i = 0; i < visible.Count; i++)
        {
            var item = visible[i];
            var row = new TaskCheckBox(() => RenameTaskFromMenu(item), () => DeleteTask(item.Id))
            {
                Text = item.Title, Checked = item.IsCompleted, Font = detailFont,
                BackColor = taskPanel.BackColor, Bounds = new Rectangle(0, Px(i * 28), width, Px(28)), Tag = item.Id,
                AccessibleName = item.Title, TabIndex = i
            };
            row.ScrollWheel = taskPanel.ScrollWheel;
            row.GotFocus += (_, _) => taskPanel.RevealRow(row);
            row.CheckedChanged += (_, _) =>
            {
                bool completed = row.Checked;
                bool changedDate = engine.Data.AdvanceChecklistDate(engine.Today);
                engine.Data.SetTaskCompletion(item.Id, completed, engine.Today);
                SaveChecklist();
                if (changedDate || engine.Data.HideCompletedTasks)
                    BeginInvoke((Action)(() => { if (!IsDisposed) { LayoutWidget(); ClampToScreen(); } }));
                else row.Invalidate();
            };
            tooltip.SetToolTip(row, item.Title + (item.IsDaily ? "\n每日任务 · 每天重置勾选" : "\n普通任务 · 跨天保留状态") + "\n右键编辑或删除");
            taskPanel.Controls.Add(row);
        }
        taskPanel.ResumeLayout(true);
        taskPanel.Configure(Px(28), scroll);
    }

    private void ChangeTaskDisplay(Action change)
    {
        change();
        SaveChecklist();
        taskPanel.ScrollTo(0);
        LayoutWidget();
        ClampToScreen();
    }

    private void ToggleTaskCollapse() => ChangeTaskDisplay(() => engine.Data.TasksCollapsed = !engine.Data.TasksCollapsed);

    internal Guid AddTask(string title, bool isDaily = false)
    {
        title = ValidateTaskTitle(title);
        var item = new ChecklistItem { Title = title, IsDaily = isDaily };
        engine.Data.Tasks!.Add(item);
        SaveChecklist();
        LayoutWidget();
        ClampToScreen();
        taskManager?.Reload(item.Id);
        return item.Id;
    }

    internal void RenameTask(Guid id, string title)
    {
        UpdateTask(id, title, engine.Data.Tasks!.Single(x => x.Id == id).IsDaily);
    }

    internal void UpdateTask(Guid id, string title, bool isDaily)
    {
        // Apply a pending date change before changing whether an existing task repeats.
        RefreshState();
        var item = engine.Data.Tasks!.Single(x => x.Id == id);
        item.Title = ValidateTaskTitle(title);
        item.IsDaily = isDaily;
        SaveChecklist();
        LayoutWidget();
        ClampToScreen();
        taskManager?.Reload(id);
    }

    internal void DeleteTask(Guid id)
    {
        engine.Data.Tasks!.RemoveAll(x => x.Id == id);
        SaveChecklist();
        LayoutWidget();
        ClampToScreen();
        taskManager?.Reload();
    }

    internal void MoveTask(Guid id, int direction)
    {
        var tasks = engine.Data.Tasks!;
        int index = tasks.FindIndex(task => task.Id == id);
        if (index < 0) return;
        int target = Math.Clamp(index + direction, 0, tasks.Count - 1);
        if (target == index) return;
        (tasks[index], tasks[target]) = (tasks[target], tasks[index]);
        SaveChecklist();
        RenderTasks();
        taskManager?.Reload(id);
    }

    private void RenameTaskFromMenu(ChecklistItem item)
    {
        using var dialog = new TaskNameForm(item.Title, item.IsDaily);
        if (dialog.ShowDialog(this) == DialogResult.OK) UpdateTask(item.Id, dialog.TaskTitle, dialog.IsDaily);
    }

    private static string ValidateTaskTitle(string title)
    {
        title = title.Trim();
        if (title.Length is < 1 or > 80) throw new ArgumentException("任务名称需要 1–80 个字符。");
        return title;
    }

    private void SaveChecklist()
    {
        engine.Settle();
        if (!TrySave()) engine.Pause();
        RefreshState();
        ArmTimer();
        Invalidate();
        taskManager?.RefreshStatuses();
    }

    internal static string HoursMinutes(long ticks, bool ceiling)
    {
        long minutes = ticks / TimeSpan.TicksPerMinute;
        if (ceiling && ticks % TimeSpan.TicksPerMinute != 0) minutes++;
        return $"{minutes / 60:00}:{minutes % 60:00}";
    }

    internal void StartTiming()
    {
        if (saveError is not null && !TrySave()) return;
        engine.Start();
        RefreshState();
        ArmTimer();
    }

    internal void PauseTiming()
    {
        engine.Pause();
        TrySave();
        RefreshState();
        ArmTimer();
    }

    internal void Switch(ActivityMode mode)
    {
        if (engine.Data.Mode == mode) return;
        engine.SwitchMode(mode);
        if (!TrySave()) engine.Pause();
        RefreshState();
        ArmTimer();
    }

    private void OnTick()
    {
        engine.Settle();
        var today = engine.Today;
        _ = engine.TodayRecord;
        // Five-second checkpoints bound crash loss without writing on every repaint.
        if (engine.IsRunning && ++runningTicks >= 5 || lastDate != today)
        {
            runningTicks = 0;
            if (!TrySave()) engine.Pause();
        }
        lastDate = today;
        RefreshState();
        if (!engine.IsRunning) ArmTimer();
    }

    private void ArmTimer()
    {
        timer.Stop();
        if (exiting) return;
        timer.Interval = engine.IsRunning ? 1000 : (int)Math.Clamp((DateTime.Today.AddDays(1) - DateTime.Now).TotalMilliseconds + 100, 1000, int.MaxValue);
        timer.Start();
    }

    private bool TrySave()
    {
        try
        {
            store.Save(engine.Data);
            saveError = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            if (saveError is null)
                tray.ShowBalloonTip(10000, "十小时 · 保存失败，计时已暂停", "内存中的进度仍保留。请释放磁盘空间或恢复目录权限，然后右键重试保存。\n" + ex.Message, ToolTipIcon.Error);
            saveError = ex.Message;
            return false;
        }
    }

    private void RefreshState()
    {
        if (engine.Data.AdvanceChecklistDate(engine.Today))
        {
            if (detailFont is not null) { LayoutWidget(); ClampToScreen(); }
            taskManager?.RefreshStatuses();
            if (!TrySave()) engine.Pause();
        }
        var day = engine.TodayRecord;
        string mode = engine.Data.Mode == ActivityMode.Work ? "工作" : "学习";
        bool reached = day.TotalTicks >= TimerEngine.GoalTicks;
        string presentation = $"{engine.Today:yyyyMMdd}|{day.TotalTicks / TimeSpan.TicksPerMinute}|{day.WorkTicks / TimeSpan.TicksPerMinute}|{day.StudyTicks / TimeSpan.TicksPerMinute}|{engine.IsRunning}|{mode}|{reached}|{saveError}";
        if (presentation != lastPresentation)
        {
            lastPresentation = presentation;
            toggle.Text = (engine.IsRunning ? "暂停" : "开始") + mode;
            UpdateModeButtons();
            tooltip.SetToolTip(toggle, mode + " · " + (engine.IsRunning ? "点击暂停" : "点击开始"));
            tray.Icon = reached ? greenIcon : redIcon;
            tray.Text = $"十小时 · {mode} · {(engine.IsRunning ? "计时中" : "已暂停")}\n今日 {HoursMinutes(day.TotalTicks, false)} / 10:00";
            Invalidate();
        }
        startItem.Enabled = !engine.IsRunning;
        pauseItem.Enabled = engine.IsRunning;
        workItem.Checked = engine.Data.Mode == ActivityMode.Work;
        studyItem.Checked = engine.Data.Mode == ActivityMode.Study;
        retryItem.Visible = saveError is not null;
    }

    private void UpdateModeButtons()
    {
        var day = engine.TodayRecord;
        void Style(Button button, ActivityMode mode, string label, long ticks, Color accent)
        {
            bool selected = engine.Data.Mode == mode;
            button.Text = (selected ? "✓ " : "") + label + "\n" + HoursMinutes(ticks, false);
            button.AccessibleName = label + "累计 " + HoursMinutes(ticks, false) + (selected ? "，当前已选" : "，点击切换");
            button.BackColor = selected
                ? (mode == ActivityMode.Work ? Color.FromArgb(29, 48, 66) : Color.FromArgb(47, 38, 65))
                : Color.FromArgb(26, 33, 44);
            button.ForeColor = selected ? accent : Muted;
            button.FlatAppearance.BorderColor = selected ? accent : Color.FromArgb(52, 64, 81);
            button.FlatAppearance.BorderSize = Math.Max(1, Px(selected ? 2 : 1));
            button.FlatAppearance.MouseOverBackColor = mode == ActivityMode.Work
                ? Color.FromArgb(36, 58, 78) : Color.FromArgb(58, 46, 79);
            button.FlatAppearance.MouseDownBackColor = button.FlatAppearance.MouseOverBackColor;
        }
        Style(workButton, ActivityMode.Work, "工作", day.WorkTicks, WorkAccent);
        Style(studyButton, ActivityMode.Study, "学习", day.StudyTicks, StudyAccent);
        Color selectedAccent = engine.Data.Mode == ActivityMode.Work ? WorkAccent : StudyAccent;
        toggle.BackColor = selectedAccent;
        toggle.ForeColor = Background;
        toggle.FlatAppearance.MouseOverBackColor = ControlPaint.Light(selectedAccent);
        toggle.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(selectedAccent);
    }

    private void ToggleStartup()
    {
        try { StartupRegistration.SetEnabled(!StartupRegistration.Enabled); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        { MessageBox.Show(this, ex.Message, "无法更改开机自启", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void OpenDataDirectory()
    {
        try
        {
            Directory.CreateDirectory(store.DirectoryPath);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(store.DirectoryPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        { MessageBox.Show(this, ex.Message, "无法打开数据目录", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void ShowHistory()
    {
        engine.Settle();
        RefreshState();
        if (history is { IsDisposed: false }) { history.Activate(); return; }
        history = new HistoryForm(engine.Data);
        history.Show(this);
    }

    private void ShowTaskManager()
    {
        RefreshState();
        if (taskManager is { IsDisposed: false }) { taskManager.Show(); taskManager.Activate(); return; }
        taskManager = new TaskManagementForm(engine.Data, (title, daily) => AddTask(title, daily), UpdateTask, DeleteTask, MoveTask);
        taskManager.Activated += (_, _) => RefreshState();
        taskManager.FormClosed += (_, _) => taskManager = null;
        taskManager.Show(this);
    }

    private void ShowManualEntry()
    {
        using var dialog = new ManualEntryForm(DateTime.Now, engine.Data.Mode, AddManualEntry);
        dialog.ShowDialog(this);
    }

    private void ShowTimeDeduction()
    {
        PauseTiming();
        using var dialog = new TimeDeductionForm(engine.Data, engine.Today, (date, mode, duration, clear) =>
        {
            engine.DeductTime(date, mode, duration, clear);
            // Applied once in memory; persistence retries must not deduct again.
            TrySave();
            history?.Close();
            RefreshState();
            ArmTimer();
        });
        dialog.ShowDialog(this);
    }

    internal void AddManualEntry(DateTime start, DateTime end, ActivityMode mode)
    {
        engine.AddManualEntry(start, end, mode);
        // The entry is already applied in memory. Close the dialog even if saving
        // fails, so retrying disk persistence cannot add the same entry twice.
        if (!TrySave()) engine.Pause();
        history?.Close();
        RefreshState();
        ArmTimer();
    }

    internal void Reveal()
    {
        ClampToScreen();
        Show();
        TopMost = true;
    }

    private void ClampToScreen()
    {
        var area = Screen.FromRectangle(Bounds).WorkingArea;
        Location = new Point(Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height)));
    }

    protected override void OnMove(EventArgs e)
    {
        base.OnMove(e);
        if (initialized) engine.Data.Position = new WindowPosition { X = Left, Y = Top };
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        base.OnResizeEnd(e);
        ClampToScreen();
        engine.Settle();
        if (!TrySave()) { engine.Pause(); RefreshState(); ArmTimer(); }
    }

    internal void ExitApplication()
    {
        engine.Pause();
        if (!TrySave())
        {
            RefreshState();
            ArmTimer();
            var answer = MessageBox.Show(this, "保存失败，退出会丢失尚未写入磁盘的进度。\n\n" + saveError + "\n\n仍要退出吗？", "十小时 · 尚未保存", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;
        }
        exiting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!exiting && e.CloseReason == CloseReason.UserClosing)
        { e.Cancel = true; Hide(); return; }
        engine.Pause();
        TrySave();
        timer.Stop();
        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.ShowMessage) { Reveal(); return; }
        if (m.Msg == NativeMethods.WM_WTSSESSION_CHANGE && (int)m.WParam is 7 or 6) PauseTiming();
        if (m.Msg == NativeMethods.WM_POWERBROADCAST && (int)m.WParam == 4) PauseTiming();
        if (m.Msg == NativeMethods.WM_TIMECHANGE && initialized) { engine.Settle(); RefreshState(); ArmTimer(); }
        if (m.Msg == 0x7E && initialized) ClampToScreen(); // WM_DISPLAYCHANGE: monitor unplugged
        if (m.Msg == NativeMethods.WM_QUERYENDSESSION) { engine.Pause(); TrySave(); m.Result = 1; return; }
        if (m.Msg == NativeMethods.WM_ENDSESSION && m.WParam != 0) { engine.Pause(); TrySave(); }
        base.WndProc(ref m);
    }

    private static Icon CreateIcon(Color color)
    {
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var background = new SolidBrush(Background);
        using var fill = new SolidBrush(color);
        g.FillEllipse(background, 1, 1, 30, 30);
        g.FillEllipse(fill, 8, 8, 16, 16);
        nint handle = bitmap.GetHicon();
        try { using var icon = Icon.FromHandle(handle); return (Icon)icon.Clone(); }
        finally { NativeMethods.DestroyIcon(handle); }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            taskDisplayMenu.Dispose();
            tooltip.Dispose();
            history?.Dispose();
            taskManager?.Dispose();
            redIcon.Dispose();
            greenIcon.Dispose();
            timeFont?.Dispose();
            detailFont?.Dispose();
        }
        base.Dispose(disposing);
    }
}
