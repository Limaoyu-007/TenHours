namespace TenHours.Core;

public enum ActivityMode { Work, Study }

public sealed class DayRecord
{
    public long WorkTicks { get; set; }
    public long StudyTicks { get; set; }
    public bool TaskHistoryRecorded { get; set; }
    public List<CompletedTaskRecord> CompletedTasks { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public long TotalTicks => checked(WorkTicks + StudyTicks);
}

public sealed class CompletedTaskRecord
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = "";
    public bool IsDaily { get; set; }
}

public sealed class WindowPosition
{
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class ManualEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public ActivityMode Mode { get; set; }
}

public sealed class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
    public bool IsDaily { get; set; }
}

public sealed class TimeDeduction
{
    public DateOnly Date { get; set; }
    public ActivityMode Mode { get; set; }
    public long Ticks { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class TimerData
{
    public int Version { get; set; } = 1;
    public ActivityMode Mode { get; set; }
    public WindowPosition? Position { get; set; }
    public List<ChecklistItem>? Tasks { get; set; }
    public bool PersonalChecklistInitialized { get; set; }
    public DateOnly? ChecklistDate { get; set; }
    public DateOnly? TaskHistoryStartDate { get; set; }
    public bool HideCompletedTasks { get; set; }
    public bool DailyTasksFirst { get; set; }
    public bool TasksCollapsed { get; set; }
    public List<ManualEntry> ManualEntries { get; set; } = [];
    public List<TimeDeduction> TimeDeductions { get; set; } = [];
    public SortedDictionary<string, DayRecord> Days { get; set; } = new(StringComparer.Ordinal);

    public bool AdvanceChecklistDate(DateTime today)
    {
        var date = DateOnly.FromDateTime(today);
        bool startHistory = TaskHistoryStartDate is null;
        if (startHistory) TaskHistoryStartDate = date;
        if (ChecklistDate == date)
        {
            if (startHistory) GetDay(today).TaskHistoryRecorded = true;
            return startHistory;
        }
        if (ChecklistDate is not null && Tasks is not null)
            foreach (var task in Tasks.Where(task => task.IsDaily)) task.IsCompleted = false;
        ChecklistDate = date;
        GetDay(today).TaskHistoryRecorded = true;
        return true;
    }

    public void SetTaskCompletion(Guid id, bool completed, DateTime today)
    {
        AdvanceChecklistDate(today);
        var task = Tasks!.Single(task => task.Id == id);
        task.IsCompleted = completed;
        var day = GetDay(today);
        day.TaskHistoryRecorded = true;
        var existing = day.CompletedTasks.FindIndex(record => record.TaskId == id);
        if (!completed)
        {
            if (existing >= 0) day.CompletedTasks.RemoveAt(existing);
        }
        else if (existing < 0)
            day.CompletedTasks.Add(new CompletedTaskRecord { TaskId = id, Title = task.Title, IsDaily = task.IsDaily });
    }

    public IReadOnlyList<ChecklistItem> VisibleTasks()
    {
        IEnumerable<ChecklistItem> items = Tasks ?? [];
        if (HideCompletedTasks) items = items.Where(task => !task.IsCompleted);
        if (DailyTasksFirst) items = items.OrderByDescending(task => task.IsDaily);
        return items.ToArray();
    }

    public DayRecord GetDay(DateTime date)
    {
        var key = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (!Days.TryGetValue(key, out var day)) Days[key] = day = new();
        return day;
    }
}
