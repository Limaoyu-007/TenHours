using System.Diagnostics;
using System.Text.Json;
using TenHours.Core;

namespace TenHours;

/// <summary>Opt-in integration probe: actual WinForms message loop; uses isolated data.</summary>
internal static class Verification
{
    internal static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var checks = new List<string>();
        var clock = new ProbeClock();
        var engine = new TimerEngine(new TimerData
        {
            Tasks = new[] { "背一组英语单词", "完成一道算法题", "读完一节技术笔记", "整理学习笔记", "修复一个问题", "练习一段代码", "准备明日计划", "整理工作材料" }
                .Select(x => new ChecklistItem { Title = x }).ToList()
        }, clock);
        var store = new JsonStore(Path.Combine(directory, "probe-data"));
        using var form = new TimerForm(engine, store);
        var layoutEvents = new List<string>();
        void ObserveLayout(string name) => layoutEvents.Add($"{DateTime.Now:HH:mm:ss.fff} {name}: controlDpi={form.DeviceDpi}, windowDpi={form.WindowDpi}, client={form.ClientSize}, bounds={form.Bounds}");
        form.DpiChanged += (_, e) => ObserveLayout($"DPI {e.DeviceDpiOld}->{e.DeviceDpiNew}");
        form.SizeChanged += (_, _) => ObserveLayout("size");
        form.LocationChanged += (_, _) => ObserveLayout("location");
        using var steps = new System.Windows.Forms.Timer { Interval = 500 };
        int step = 0;
        var process = Process.GetCurrentProcess();
        TimeSpan idleCpu = default;
        long idleStamp = 0;
        long initialPrivate = 0;
        object? idleMetrics = null;
        TimeSpan activeCpu = default;
        long activeStamp = 0;
        long activeTicks = 0;
        Exception? failure = null;
        void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException(description);
            checks.Add(description);
        }
        void Capture(string name, Form target)
        {
            using var bitmap = new Bitmap(target.Width, target.Height);
            target.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(directory, name + ".png"));
        }
        steps.Tick += (_, _) =>
        {
            try
            {
                switch (step++)
                {
                    case 0:
                        Check(form.TopMost && !form.ShowInTaskbar && form.FormBorderStyle == FormBorderStyle.None, "borderless, topmost, absent from taskbar");
                        Check(form.ClientSize.Width == (int)Math.Round(TimerForm.LogicalWidth * form.WindowDpi / 96d), "compact checklist widget width at actual window DPI");
                        Capture("01-paused", form);
                        form.Controls.OfType<Button>().Single(x => x.Name == "ToggleTimer").PerformClick();
                        Check(engine.IsRunning, "real button starts timing");
                        clock.Advance(5);
                        engine.Settle();
                        Check(engine.Data.Days["2026-10-20"].TotalTicks == 3 * TimeSpan.TicksPerSecond && engine.TodayRecord.TotalTicks == 2 * TimeSpan.TicksPerSecond, "running interval split at midnight");
                        form.Switch(ActivityMode.Study);
                        break;
                    case 1:
                        form.Hide(); clock.Advance(60); engine.Settle();
                        Check(!form.Visible && engine.TodayRecord.StudyTicks == TimeSpan.TicksPerMinute, "hidden window keeps timing");
                        form.Reveal();
                        NativeMethods.SendMessage(form.Handle, NativeMethods.WM_WTSSESSION_CHANGE, 7, 0);
                        Check(!engine.IsRunning, "lock Windows message pauses and saves");
                        clock.Advance(120);
                        NativeMethods.SendMessage(form.Handle, NativeMethods.WM_WTSSESSION_CHANGE, 8, 0);
                        Check(!engine.IsRunning, "unlock requires manual resume");
                        Check(store.Load().Data.GetDay(engine.Today).TotalTicks == 62 * TimeSpan.TicksPerSecond, "lock checkpoint persisted without locked time");
                        form.StartTiming(); clock.Advance(10);
                        NativeMethods.SendMessage(form.Handle, NativeMethods.WM_POWERBROADCAST, 4, 0);
                        Check(!engine.IsRunning, "suspend Windows message pauses");
                        NativeMethods.SendMessage(form.Handle, NativeMethods.WM_POWERBROADCAST, 18, 0);
                        Check(!engine.IsRunning, "resume does not auto-start");
                        form.StartTiming(); clock.Advance(36000); engine.Settle();
                        form.Switch(ActivityMode.Work);
                        form.Refresh();
                        Capture("02-achieved-running", form);
                        Check(engine.TodayRecord.TotalTicks > TimerEngine.GoalTicks, "continued accumulation beyond target");
                        break;
                    case 2:
                        form.PauseTiming();
                        using (var history = new HistoryForm(engine.Data))
                        { history.Show(form); history.Refresh(); Capture("03-history", history); history.Close(); }
                        form.Location = new Point(44, 66);
                        form.PauseTiming();
                        Check(store.Load().Data.Position is { X: 44, Y: 66 }, "window position persisted");
                        Check(!new TimerEngine(store.Load().Data, clock).IsRunning, "reopened history starts paused");
                        form.Close();
                        Check(!form.Visible && !form.IsDisposed, "Alt-F4/close hides to tray");
                        NativeMethods.SendMessage(form.Handle, (int)NativeMethods.ShowMessage, 0, 0);
                        Check(form.Visible, "single-instance show message restores hidden widget");
                        // Give JIT and drawing a moment to settle before the idle sample.
                        break;
                    case 3:
                        form.StartTiming();
                        clock.Advance(5);
                        using (var locked = new FileStream(store.DataPath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            form.Switch(ActivityMode.Study);
                            Check(!engine.IsRunning, "failed save automatically pauses without losing in-memory progress");
                            form.Refresh(); Capture("04-save-error", form);
                        }
                        form.PauseTiming();
                        Check(store.Load().Data.GetDay(engine.Today).TotalTicks == engine.TodayRecord.TotalTicks, "retry after save failure retains accumulated time");
                        break;
                    case 85:
                        File.WriteAllText(Path.Combine(directory, "layout-state.json"), JsonSerializer.Serialize(new
                        {
                            form.DeviceDpi, form.WindowDpi, Width = form.ClientSize.Width, Height = form.ClientSize.Height,
                            Awareness = Application.HighDpiMode.ToString(), Events = layoutEvents
                        }, new JsonSerializerOptions { WriteIndented = true }));
                        Check(form.ClientSize.Width == (int)Math.Round(TimerForm.LogicalWidth * form.WindowDpi / 96d)
                            && form.ClientSize.Height == (int)Math.Round(form.LogicalHeight * form.WindowDpi / 96d), $"logical size retained after DPI/position change (windowDpi={form.WindowDpi}, client={form.ClientSize})");
                        Capture("05-settled-layout", form);
                        process.Refresh(); idleCpu = process.TotalProcessorTime; idleStamp = Stopwatch.GetTimestamp(); initialPrivate = process.PrivateMemorySize64;
                        break;
                    case 4:
                        clock.Advance(8 * 3600); // Paused; make 09:30-15:30 a past interval today.
                        long before = engine.TodayRecord.TotalTicks;
                        using (var manual = new ManualEntryForm(clock.LocalNow.DateTime, ActivityMode.Study, form.AddManualEntry))
                        {
                            manual.Show(form);
                            manual.SetInput(engine.Today.AddHours(9.5), engine.Today.AddHours(15.5), ActivityMode.Study);
                            Check(manual.PreviewText.Contains("6 小时 00 分钟"), "manual dialog previews 09:30-15:30 as six hours");
                            manual.Refresh(); Capture("06-manual-entry", manual);
                            manual.ConfirmInput();
                            Check(manual.DialogResult == DialogResult.OK, "manual entry submitted via the real confirm button");
                        }
                        Check(engine.TodayRecord.TotalTicks == before + 21600L * TimeSpan.TicksPerSecond, "manual entry adds six hours on top of existing time");
                        Check(store.Load().Data.ManualEntries.Count == 1 && store.Load().Data.GetDay(engine.Today).TotalTicks == engine.TodayRecord.TotalTicks,
                            "manual metadata and updated totals saved immediately");
                        form.Refresh(); Capture("07-after-manual-entry", form);
                        using (var history = new HistoryForm(engine.Data))
                        { history.Show(form); history.Refresh(); Capture("08-history-with-manual", history); history.Close(); }
                        break;
                    case 115:
                        process.Refresh();
                        idleMetrics = new
                        {
                            IdleSeconds = Stopwatch.GetElapsedTime(idleStamp).TotalSeconds,
                            IdleCpuMilliseconds = (process.TotalProcessorTime - idleCpu).TotalMilliseconds,
                            WorkingSetMiB = process.WorkingSet64 / 1048576d,
                            PrivateMiB = process.PrivateMemorySize64 / 1048576d,
                            PrivateGrowthBytes = process.PrivateMemorySize64 - initialPrivate
                        };
                        clock.EnableLive();
                        form.StartTiming();
                        activeTicks = engine.TodayRecord.TotalTicks;
                        activeStamp = Stopwatch.GetTimestamp();
                        activeCpu = process.TotalProcessorTime;
                        break;
                    case 5:
                        Check(form.TaskRows.Count == 8, "task checklist is embedded below the timer with scrollable rows");
                        var first = form.TaskRows[0];
                        first.AccessibilityObject.DoDefaultAction();
                        Check(first.Checked && store.Load().Data.Tasks!.Single(x => x.Id == (Guid)first.Tag!).IsCompleted,
                            "real checkbox toggles and saves completed state immediately");
                        long totalBeforeChecklist = engine.TodayRecord.TotalTicks;
                        var addedId = form.AddTask("一项临时任务");
                        form.RenameTask(addedId, "更新后的任务");
                        Check(store.Load().Data.Tasks!.Single(x => x.Id == addedId).Title == "更新后的任务", "task can be added and renamed");
                        form.DeleteTask(addedId);
                        Check(store.Load().Data.Tasks!.Count == 8 && engine.TodayRecord.TotalTicks == totalBeforeChecklist,
                            "deleting a task leaves all timer totals unchanged");
                        form.Refresh(); Capture("09-checklist", form);
                        using (var taskName = new TaskNameForm("读一页书"))
                        {
                            taskName.Show(form); taskName.Refresh(); Capture("10-task-name", taskName);
                            taskName.AcceptButton!.PerformClick();
                            Check(taskName.DialogResult == DialogResult.OK && taskName.TaskTitle == "读一页书", $"task name dialog retains confirmed reminder (result={taskName.DialogResult}, title={taskName.TaskTitle})");
                        }
                        break;
                    case 145:
                        engine.Settle();
                        double seconds = Stopwatch.GetElapsedTime(activeStamp).TotalSeconds;
                        double accumulated = (engine.TodayRecord.TotalTicks - activeTicks) / (double)TimeSpan.TicksPerSecond;
                        Check(Math.Abs(seconds - accumulated) < .25, "actual elapsed seconds survive one-second UI refresh and five-second saves");
                        form.PauseTiming();
                        process.Refresh();
                        Check(store.Load().Data.GetDay(engine.Today).TotalTicks == engine.TodayRecord.TotalTicks, "real-time run persisted on pause");
                        var metrics = new
                        {
                            Checks = checks, WindowDpi = form.WindowDpi, ThreadContextDeviceDpi = form.DeviceDpi, Idle = idleMetrics,
                            Running = new
                            {
                                Seconds = seconds, AccumulatedSeconds = accumulated,
                                CpuMilliseconds = (process.TotalProcessorTime - activeCpu).TotalMilliseconds,
                                WorkingSetMiB = process.WorkingSet64 / 1048576d,
                                PrivateMiB = process.PrivateMemorySize64 / 1048576d
                            },
                            Note = "Short probe, not a ten-hour soak or physical suspend/lock test."
                        };
                        File.WriteAllText(Path.Combine(directory, "ui-report.json"), JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true }));
                        steps.Stop(); form.ExitApplication();
                        break;
                }
            }
            catch (Exception ex)
            {
                failure = ex; steps.Stop();
                File.WriteAllText(Path.Combine(directory, "failure.txt"), ex + "\n" + string.Join("\n", checks));
                form.ExitApplication();
            }
        };
        form.Shown += (_, _) => steps.Start();
        Application.Run(form);
        if (failure is not null) Environment.ExitCode = 1;
    }

    private sealed class ProbeClock : IClock
    {
        private DateTimeOffset wall = DateTimeOffset.Parse("2026-10-20T23:59:57+08:00");
        private long ticks;
        private long? liveAnchor;
        private TimeSpan LiveElapsed => liveAnchor is { } start ? Stopwatch.GetElapsedTime(start) : TimeSpan.Zero;
        public DateTimeOffset LocalNow => wall + LiveElapsed;
        public long Timestamp => ticks + LiveElapsed.Ticks;
        public TimeSpan Elapsed(long start, long end) => TimeSpan.FromTicks(end - start);
        internal void Advance(int seconds) { var delta = TimeSpan.FromSeconds(seconds); ticks += delta.Ticks; wall += delta; }
        internal void EnableLive() => liveAnchor = Stopwatch.GetTimestamp();
    }
}
