using System.Diagnostics;

namespace TenHours.Core;

public interface IClock
{
    DateTimeOffset LocalNow { get; }
    long Timestamp { get; }
    TimeSpan Elapsed(long start, long end);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset LocalNow => DateTimeOffset.Now;
    public long Timestamp => Stopwatch.GetTimestamp();
    public TimeSpan Elapsed(long start, long end) => Stopwatch.GetElapsedTime(start, end);
}

/// <summary>Single UI-thread engine. UI tick frequency never determines elapsed time.</summary>
public sealed class TimerEngine(TimerData data, IClock clock)
{
    public const long GoalTicks = 36_000L * TimeSpan.TicksPerSecond;
    private long anchorStamp;
    private DateTimeOffset anchorWall;
    public TimerData Data { get; } = data;
    public bool IsRunning { get; private set; }
    public DateTime Today => clock.LocalNow.Date;
    public DayRecord TodayRecord => Data.GetDay(Today);

    public void Start()
    {
        if (IsRunning) return;
        anchorStamp = clock.Timestamp;
        anchorWall = clock.LocalNow;
        IsRunning = true;
    }

    public void Pause()
    {
        Settle();
        IsRunning = false;
    }

    public void SwitchMode(ActivityMode mode)
    {
        Settle();
        Data.Mode = mode;
    }

    /// <summary>Local wall-clock entry, deliberately additive even when intervals overlap.</summary>
    public ManualEntry AddManualEntry(DateTime start, DateTime end, ActivityMode mode)
    {
        start = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
        end = DateTime.SpecifyKind(end, DateTimeKind.Unspecified);
        if (!Enum.IsDefined(mode)) throw new ArgumentException("请选择工作或学习。", nameof(mode));
        if (end <= start) throw new ArgumentException("结束时间必须晚于开始时间。跨零点请勾选「结束时间在次日」。");
        if (end - start > TimeSpan.FromDays(1)) throw new ArgumentException("一次最多补录 24 小时，请按日期分次补录。");
        if (end > clock.LocalNow.DateTime) throw new ArgumentException("只能补录已经结束的时段，结束时间不能晚于现在。");
        Settle();
        // Validate all affected totals before changing any of them.
        var additions = new List<(DateTime Date, long Ticks)>();
        for (var cursor = start; cursor < end;)
        {
            var boundary = cursor.Date.AddDays(1);
            var until = end < boundary ? end : boundary;
            long ticks = (until - cursor).Ticks;
            var key = cursor.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (Data.Days.TryGetValue(key, out var existing) && existing.TotalTicks > long.MaxValue - ticks)
                throw new ArgumentException("当日累计时长超出支持范围。");
            additions.Add((cursor.Date, ticks));
            cursor = until;
        }
        var entry = new ManualEntry { Start = start, End = end, Mode = mode };
        foreach (var (date, ticks) in additions)
        {
            var day = Data.GetDay(date);
            if (mode == ActivityMode.Work) day.WorkTicks += ticks;
            else day.StudyTicks += ticks;
        }
        Data.ManualEntries.Add(entry);
        return entry;
    }

    public void Settle()
    {
        if (!IsRunning) return;
        long stamp = clock.Timestamp;
        var now = clock.LocalNow;
        var elapsed = clock.Elapsed(anchorStamp, stamp);
        if (elapsed < TimeSpan.Zero) throw new InvalidOperationException("Monotonic clock moved backwards.");
        // Allocate by the monotonic timeline. A manual/NTP clock change only rebases
        // the next interval; it can never create or subtract elapsed time.
        Allocate(anchorWall, elapsed.Ticks);
        var projected = anchorWall + elapsed;
        anchorWall = now.Offset != projected.Offset || (now - projected).Duration() > TimeSpan.FromSeconds(2)
            ? now : projected;
        anchorStamp = stamp;
    }

    private void Allocate(DateTimeOffset from, long ticks)
    {
        while (ticks > 0)
        {
            var midnight = new DateTimeOffset(from.Date.AddDays(1), from.Offset);
            long part = Math.Min(ticks, (midnight - from).Ticks);
            var day = Data.GetDay(from.Date);
            if (Data.Mode == ActivityMode.Work) day.WorkTicks = checked(day.WorkTicks + part);
            else day.StudyTicks = checked(day.StudyTicks + part);
            ticks -= part;
            from += TimeSpan.FromTicks(part);
        }
    }
}
