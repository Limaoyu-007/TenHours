using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenHours.Core;

public sealed record LoadResult(TimerData Data, string? Warning = null);

/// <summary>Same-volume durable temp write + atomic rename/replace; retain the last good copy.</summary>
public sealed class JsonStore(string directory)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter<ActivityMode>() }
    };
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    public string DataPath => Path.Combine(DirectoryPath, "data.json");
    public string BackupPath => DataPath + ".bak";
    private bool preserveBackup;

    public LoadResult Load()
    {
        if (!File.Exists(DataPath) && !File.Exists(BackupPath)) return new(new());
        Exception? primaryError = null;
        try
        {
            return new(Read(DataPath));
        }
        catch (Exception ex) when (IsDataError(ex)) { primaryError = ex; }
        try
        {
            var recovered = Read(BackupPath);
            preserveBackup = true;
            return new(recovered, "主数据文件不可读，已恢复上一份完整备份。原文件将在下次保存时保留为 data.damaged-*.json。");
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            throw new InvalidDataException("数据文件及备份均无法读取。为保护历史记录，程序不会覆盖它们。\n数据目录：" + DirectoryPath, primaryError);
        }
    }

    public void Save(TimerData data)
    {
        Directory.CreateDirectory(DirectoryPath);
        string temp = Path.Combine(DirectoryPath, ".data-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, data, Options);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(DataPath))
            {
                if (preserveBackup)
                {
                    // Preserve the corrupt primary atomically without overwriting the good backup.
                    string damaged = Path.Combine(DirectoryPath, "data.damaged-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json");
                    File.Replace(temp, DataPath, damaged);
                }
                else File.Replace(temp, DataPath, BackupPath);
            }
            else File.Move(temp, DataPath);
            preserveBackup = false;
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static TimerData Read(string path)
    {
        using var stream = File.OpenRead(path);
        var data = JsonSerializer.Deserialize<TimerData>(stream, Options) ?? throw new InvalidDataException("Empty JSON.");
        if (data.Version != 1) throw new InvalidDataException("Unsupported data version: " + data.Version);
        if (!Enum.IsDefined(data.Mode) || data.Days is null || data.ManualEntries is null) throw new InvalidDataException("Invalid settings.");
        var manualIds = new HashSet<Guid>();
        if (data.Tasks is not null)
        {
            var taskIds = new HashSet<Guid>();
            foreach (var task in data.Tasks)
            {
                if (task is null || task.Id == Guid.Empty || !taskIds.Add(task.Id)
                    || string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 80)
                    throw new InvalidDataException("Invalid checklist item.");
            }
        }
        foreach (var entry in data.ManualEntries)
        {
            if (entry is null || entry.Id == Guid.Empty || !manualIds.Add(entry.Id) || !Enum.IsDefined(entry.Mode)
                || entry.End <= entry.Start || entry.End - entry.Start > TimeSpan.FromDays(1))
                throw new InvalidDataException("Invalid manual entry.");
        }
        foreach (var (date, day) in data.Days)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _) || day is null || day.WorkTicks < 0 || day.StudyTicks < 0 ||
                day.WorkTicks > long.MaxValue - day.StudyTicks)
                throw new InvalidDataException("Invalid day record: " + date);
            if (day.CompletedTasks is null) throw new InvalidDataException("Invalid task history: " + date);
            var completedIds = new HashSet<Guid>();
            foreach (var task in day.CompletedTasks)
                if (task is null || task.TaskId == Guid.Empty || !completedIds.Add(task.TaskId)
                    || string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 80)
                    throw new InvalidDataException("Invalid completed task: " + date);
        }
        return data;
    }

    private static bool IsDataError(Exception ex) => ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException;
}
