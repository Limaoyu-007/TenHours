using TenHours.Core;

int passed = 0;
void Test(string name, Action body)
{
    body();
    passed++;
    Console.WriteLine("PASS " + name);
}
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
}
long Sec(double value) => TimeSpan.FromSeconds(value).Ticks;
(TimerEngine engine, FakeClock clock) Create(string at = "2026-10-20T09:00:00+08:00")
{
    var clock = new FakeClock(DateTimeOffset.Parse(at));
    return (new TimerEngine(new(), clock), clock);
}

Test("paused time excluded; resume preserves fractional seconds", () =>
{
    var (e, c) = Create();
    c.Advance(100);
    e.Start(); c.Advance(.25); e.Pause(); c.Advance(900);
    e.Start(); c.Advance(.75); e.Pause();
    Equal(Sec(1), e.TodayRecord.TotalTicks);
});
Test("delayed UI ticks do not drift; switching settles to previous mode", () =>
{
    var (e, c) = Create();
    e.Start(); c.Advance(123.456); e.SwitchMode(ActivityMode.Study);
    c.Advance(234.567); e.Pause();
    Equal(Sec(123.456), e.TodayRecord.WorkTicks);
    Equal(Sec(234.567), e.TodayRecord.StudyTicks);
});
Test("midnight splits both dates and leaves timer running", () =>
{
    var (e, c) = Create("2026-10-20T23:59:58.500+08:00");
    e.Start(); c.Advance(3); e.Settle();
    Equal(Sec(1.5), e.Data.Days["2026-10-20"].TotalTicks);
    Equal(Sec(1.5), e.Data.Days["2026-10-21"].TotalTicks);
    Equal(true, e.IsRunning);
});
Test("delayed update can cross multiple days without dropping time", () =>
{
    var (e, c) = Create("2026-10-20T23:00:00+08:00");
    e.Start(); c.Advance(90000); e.Pause();
    Equal(Sec(3600), e.Data.Days["2026-10-20"].TotalTicks);
    Equal(Sec(86400), e.Data.Days["2026-10-21"].TotalTicks);
});
Test("manual clock jumps forward and backward never change elapsed time", () =>
{
    var (e, c) = Create();
    e.Start(); c.Advance(10); c.Wall += TimeSpan.FromHours(4); e.Settle();
    c.Advance(5); c.Wall -= TimeSpan.FromHours(6); e.Settle();
    c.Advance(7); e.Pause();
    Equal(Sec(22), e.TodayRecord.TotalTicks);
});
Test("goal threshold is exact and time continues after ten hours", () =>
{
    var (e, c) = Create();
    e.Start(); c.Advance(35999.9); e.Settle();
    Equal(true, e.TodayRecord.TotalTicks < TimerEngine.GoalTicks);
    c.Advance(.1); e.Settle(); Equal(TimerEngine.GoalTicks, e.TodayRecord.TotalTicks);
    c.Advance(600); e.Pause(); Equal(Sec(36600), e.TodayRecord.TotalTicks);
});
Test("pause is idempotent; a new engine never resumes offline time", () =>
{
    var (e, c) = Create();
    e.Start(); c.Advance(30); e.Pause(); e.Pause();
    var reopened = new TimerEngine(e.Data, c);
    c.Advance(3600); reopened.Settle();
    Equal(false, reopened.IsRunning); Equal(Sec(30), reopened.TodayRecord.TotalTicks);
});
Test("DST offset change keeps monotonic total", () =>
{
    var (e, c) = Create("2026-11-01T01:59:59-04:00");
    e.Start(); c.Advance(2); c.Wall = c.Wall.ToOffset(TimeSpan.FromHours(-5)); e.Settle();
    c.Advance(2); e.Pause(); Equal(Sec(4), e.TodayRecord.TotalTicks);
});

Test("09:30 to 15:30 adds six hours to the existing two, with separate modes", () =>
{
    var (e, _) = Create("2026-10-20T18:00:00+08:00");
    e.Data.Mode = ActivityMode.Study;
    e.TodayRecord.StudyTicks = Sec(7200);
    var entry = e.AddManualEntry(new(2026, 10, 20, 9, 30, 0), new(2026, 10, 20, 15, 30, 0), ActivityMode.Work);
    Equal(Sec(28800), e.TodayRecord.TotalTicks);
    Equal(Sec(21600), e.TodayRecord.WorkTicks);
    Equal(Sec(7200), e.TodayRecord.StudyTicks);
    Equal(ActivityMode.Study, e.Data.Mode);
    Equal(entry.Id, e.Data.ManualEntries.Single().Id);
    // Overlap is explicitly additive, including an intentionally repeated interval.
    e.AddManualEntry(entry.Start, entry.End, ActivityMode.Work);
    Equal(Sec(50400), e.TodayRecord.TotalTicks);
    Equal(2, e.Data.ManualEntries.Select(x => x.Id).Distinct().Count());
});
Test("manual entry can reach the target without starting the live timer", () =>
{
    var (e, _) = Create("2026-10-20T18:00:00+08:00");
    e.TodayRecord.WorkTicks = Sec(14400);
    e.AddManualEntry(new(2026, 10, 20, 9, 30, 0), new(2026, 10, 20, 15, 30, 0), ActivityMode.Study);
    Equal(TimerEngine.GoalTicks, e.TodayRecord.TotalTicks);
    Equal(false, e.IsRunning);
});
Test("backfill crosses midnight into the correct dates without changing today", () =>
{
    var (e, _) = Create("2026-10-22T18:00:00+08:00");
    e.AddManualEntry(new(2026, 10, 20, 23, 30, 0), new(2026, 10, 21, 1, 30, 0), ActivityMode.Study);
    Equal(Sec(1800), e.Data.Days["2026-10-20"].StudyTicks);
    Equal(Sec(5400), e.Data.Days["2026-10-21"].StudyTicks);
    Equal(0L, e.TodayRecord.TotalTicks);
});
Test("backfill settles running time, preserves running state and active mode", () =>
{
    var (e, c) = Create("2026-10-20T18:00:00+08:00");
    e.Start(); c.Advance(15);
    e.AddManualEntry(new(2026, 10, 20, 9, 30, 0), new(2026, 10, 20, 15, 30, 0), ActivityMode.Study);
    Equal(true, e.IsRunning);
    c.Advance(5); e.Pause();
    Equal(Sec(20), e.TodayRecord.WorkTicks);
    Equal(Sec(21600), e.TodayRecord.StudyTicks);
});
Test("invalid/future/over-24-hour entries leave all manual data untouched", () =>
{
    var (e, _) = Create("2026-10-20T18:00:00+08:00");
    var from = new DateTime(2026, 10, 20, 9, 30, 0);
    var invalid = new[] { (from, from), (from, from.AddMinutes(-1)),
        (from, new DateTime(2026, 10, 20, 18, 1, 0)), (from.AddDays(-2), from) };
    foreach (var (start, end) in invalid)
    {
        bool rejected = false;
        try { e.AddManualEntry(start, end, ActivityMode.Work); } catch (ArgumentException) { rejected = true; }
        Equal(true, rejected);
    }
    Equal(0, e.Data.ManualEntries.Count);
    Equal(0, e.Data.Days.Count);
});

string directory = Path.Combine(args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetTempPath(), "TenHours-tests-" + Guid.NewGuid().ToString("N"));
try
{
    Test("JSON roundtrip, previous complete backup, no stale temp files", () =>
    {
        var store = new JsonStore(directory);
        var data = store.Load().Data;
        data.GetDay(new DateTime(2026, 10, 20)).WorkTicks = Sec(17.5);
        data.Mode = ActivityMode.Study;
        data.Position = new() { X = -1200, Y = 200 };
        store.Save(data);
        data.GetDay(new DateTime(2026, 10, 20)).StudyTicks = Sec(20);
        store.Save(data);
        var roundtrip = store.Load().Data;
        Equal(Sec(37.5), roundtrip.Days["2026-10-20"].TotalTicks);
        Equal(-1200, roundtrip.Position!.X);
        Equal(ActivityMode.Study, roundtrip.Mode);
        Equal(true, File.Exists(store.BackupPath));
        Equal(0, Directory.GetFiles(directory, "*.tmp").Length);
    });
    Test("failed atomic replacement retains the previous data and removes temporary files", () =>
    {
        var store = new JsonStore(directory);
        var data = store.Load().Data;
        data.GetDay(new DateTime(2026, 10, 20)).StudyTicks = Sec(100);
        bool rejected = false;
        using (var locked = new FileStream(store.DataPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            try { store.Save(data); } catch (IOException) { rejected = true; }
        }
        Equal(true, rejected);
        Equal(Sec(37.5), store.Load().Data.Days["2026-10-20"].TotalTicks);
        Equal(0, Directory.GetFiles(directory, "*.tmp").Length);
    });
    Test("truncated primary recovers backup and preserves damaged evidence", () =>
    {
        var store = new JsonStore(directory);
        File.WriteAllText(store.DataPath, "{truncated");
        var recovered = store.Load();
        Equal(true, recovered.Warning is not null);
        Equal(Sec(17.5), recovered.Data.Days["2026-10-20"].TotalTicks);
        store.Save(recovered.Data);
        Equal(1, Directory.GetFiles(directory, "data.damaged-*.json").Length);
        Equal(Sec(17.5), store.Load().Data.Days["2026-10-20"].TotalTicks);
    });
    Test("invalid negative history is rejected and restored from backup", () =>
    {
        var store = new JsonStore(directory);
        File.WriteAllText(store.DataPath, "{\"Version\":1,\"Days\":{\"2026-10-20\":{\"WorkTicks\":-1}}}");
        Equal(true, store.Load().Warning is not null);
    });
    Test("both files corrupt: fail closed without overwriting either", () =>
    {
        var store = new JsonStore(directory);
        File.WriteAllText(store.DataPath, "bad primary"); File.WriteAllText(store.BackupPath, "bad backup");
        bool rejected = false;
        try { store.Load(); } catch (InvalidDataException) { rejected = true; }
        Equal(true, rejected);
        Equal("bad primary", File.ReadAllText(store.DataPath));
        Equal("bad backup", File.ReadAllText(store.BackupPath));
    });
    Test("manual interval metadata and totals survive restart without being reapplied", () =>
    {
        var store = new JsonStore(Path.Combine(directory, "manual"));
        var (e, c) = Create("2026-10-20T18:00:00+08:00");
        var entry = e.AddManualEntry(new(2026, 10, 20, 9, 30, 0), new(2026, 10, 20, 15, 30, 0), ActivityMode.Work);
        store.Save(e.Data);
        var loaded = store.Load().Data;
        Equal(entry.Id, loaded.ManualEntries.Single().Id);
        Equal(entry.Start, loaded.ManualEntries.Single().Start);
        Equal(entry.End, loaded.ManualEntries.Single().End);
        var reopened = new TimerEngine(loaded, c);
        reopened.Settle();
        Equal(Sec(21600), reopened.TodayRecord.WorkTicks);
    });
    Test("existing v1 JSON without manual entries remains readable", () =>
    {
        var store = new JsonStore(Path.Combine(directory, "legacy"));
        Directory.CreateDirectory(store.DirectoryPath);
        File.WriteAllText(store.DataPath, "{\"Version\":1,\"Days\":{\"2026-10-20\":{\"WorkTicks\":100}}}");
        var loaded = store.Load().Data;
        Equal(100L, loaded.Days["2026-10-20"].WorkTicks);
        Equal(0, loaded.ManualEntries.Count);
        Equal<List<ChecklistItem>?>(null, loaded.Tasks);
    });
    Test("checklist completion survives restart and midnight independently from timer totals", () =>
    {
        var store = new JsonStore(Path.Combine(directory, "checklist"));
        var (e, c) = Create("2026-10-20T18:00:00+08:00");
        e.TodayRecord.WorkTicks = Sec(7200);
        var item = new ChecklistItem { Title = "复习英语单词", IsCompleted = true };
        e.Data.Tasks = [item];
        store.Save(e.Data);
        var reopened = new TimerEngine(store.Load().Data, c);
        c.Advance(86400);
        Equal(true, reopened.Data.Tasks!.Single().IsCompleted);
        Equal(item.Id, reopened.Data.Tasks!.Single().Id);
        Equal(0L, reopened.TodayRecord.TotalTicks);
        Equal(Sec(7200), reopened.Data.Days["2026-10-20"].TotalTicks);
        reopened.Data.Tasks!.Clear(); store.Save(reopened.Data);
        Equal(0, store.Load().Data.Tasks!.Count);
    });
    Test("task completion snapshots survive edits, deletion, restart and daily reset", () =>
    {
        var store = new JsonStore(Path.Combine(directory, "task-history"));
        var today = new DateTime(2026, 10, 25);
        var daily = new ChecklistItem { Title = "英语单词", IsDaily = true };
        var normal = new ChecklistItem { Title = "项目收尾" };
        var data = new TimerData { Tasks = [daily, normal] };
        data.GetDay(today.AddDays(-1)).WorkTicks = Sec(7200);
        data.AdvanceChecklistDate(today);
        Equal(false, data.GetDay(today.AddDays(-1)).TaskHistoryRecorded);
        data.SetTaskCompletion(daily.Id, true, today);
        data.SetTaskCompletion(normal.Id, true, today);
        daily.Title = "改过名字";
        data.Tasks.Remove(normal);
        store.Save(data);
        var loaded = store.Load().Data;
        Equal(2, loaded.GetDay(today).CompletedTasks.Count);
        Equal("英语单词", loaded.GetDay(today).CompletedTasks[0].Title);
        loaded.AdvanceChecklistDate(today.AddDays(3));
        Equal(false, loaded.Tasks![0].IsCompleted);
        Equal(2, loaded.GetDay(today).CompletedTasks.Count);
        Equal(0, loaded.GetDay(today.AddDays(3)).CompletedTasks.Count);
        Equal(Sec(7200), loaded.GetDay(today.AddDays(-1)).WorkTicks);
    });
    Test("completion undo affects only today and never duplicates an entry", () =>
    {
        var today = new DateTime(2026, 10, 25);
        var task = new ChecklistItem { Title = "算法题", IsDaily = true };
        var data = new TimerData { Tasks = [task] };
        data.SetTaskCompletion(task.Id, true, today);
        data.SetTaskCompletion(task.Id, true, today);
        Equal(1, data.GetDay(today).CompletedTasks.Count);
        data.SetTaskCompletion(task.Id, false, today);
        Equal(0, data.GetDay(today).CompletedTasks.Count);
        data.SetTaskCompletion(task.Id, true, today);
        data.SetTaskCompletion(task.Id, false, today.AddDays(1));
        Equal(1, data.GetDay(today).CompletedTasks.Count);
        Equal(0, data.GetDay(today.AddDays(1)).CompletedTasks.Count);
    });
    Test("display preferences persist without changing the managed task order", () =>
    {
        var store = new JsonStore(Path.Combine(directory, "task-display"));
        var normal = new ChecklistItem { Title = "项目" };
        var daily = new ChecklistItem { Title = "英语", IsDaily = true };
        var completed = new ChecklistItem { Title = "已完成", IsCompleted = true };
        var data = new TimerData { Tasks = [normal, daily, completed], HideCompletedTasks = true,
            DailyTasksFirst = true, TasksCollapsed = true };
        Equal(daily.Id, data.VisibleTasks()[0].Id);
        Equal(2, data.VisibleTasks().Count);
        Equal(normal.Id, data.Tasks[0].Id);
        store.Save(data);
        var loaded = store.Load().Data;
        Equal(true, loaded.TasksCollapsed);
        Equal(true, loaded.HideCompletedTasks);
        Equal(true, loaded.DailyTasksFirst);
        Equal(daily.Id, loaded.VisibleTasks()[0].Id);
    });
    Test("invalid checklist data recovers the last complete backup", () =>
    {
        var store = new JsonStore(Path.Combine(directory, "invalid-checklist"));
        var data = new TimerData { Tasks = [new ChecklistItem { Title = "有效任务" }] };
        store.Save(data); store.Save(data);
        File.WriteAllText(store.DataPath, "{\"Version\":1,\"Tasks\":[{\"Id\":\"00000000-0000-0000-0000-000000000000\",\"Title\":\"\"}]}");
        var recovered = store.Load();
        Equal(true, recovered.Warning is not null);
        Equal("有效任务", recovered.Data.Tasks!.Single().Title);
    });
}
finally { Directory.Delete(directory, recursive: true); }
Console.WriteLine($"{passed} tests passed.");

sealed class FakeClock(DateTimeOffset wall) : IClock
{
    public DateTimeOffset Wall = wall;
    private long stamp;
    public DateTimeOffset LocalNow => Wall;
    public long Timestamp => stamp;
    public TimeSpan Elapsed(long start, long end) => TimeSpan.FromTicks(end - start);
    public void Advance(double seconds)
    {
        var duration = TimeSpan.FromSeconds(seconds);
        stamp += duration.Ticks;
        Wall += duration;
    }
}
