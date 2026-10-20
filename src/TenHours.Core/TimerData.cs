namespace TenHours.Core;

public enum ActivityMode { Work, Study }

public sealed class DayRecord
{
    public long WorkTicks { get; set; }
    public long StudyTicks { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public long TotalTicks => checked(WorkTicks + StudyTicks);
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
}

public sealed class TimerData
{
    public int Version { get; set; } = 1;
    public ActivityMode Mode { get; set; }
    public WindowPosition? Position { get; set; }
    public List<ChecklistItem>? Tasks { get; set; }
    public List<ManualEntry> ManualEntries { get; set; } = [];
    public SortedDictionary<string, DayRecord> Days { get; set; } = new(StringComparer.Ordinal);

    public DayRecord GetDay(DateTime date)
    {
        var key = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (!Days.TryGetValue(key, out var day)) Days[key] = day = new();
        return day;
    }
}
