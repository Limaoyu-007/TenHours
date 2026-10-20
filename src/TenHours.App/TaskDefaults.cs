using TenHours.Core;

namespace TenHours;

internal static class TaskDefaults
{
    internal static List<ChecklistItem> Create()
    {
        // Optional first-run seed, kept outside the executable and source repository.
        string path = Path.Combine(AppContext.BaseDirectory, "tasks.txt");
        string[] titles = File.Exists(path) ? File.ReadAllLines(path)
            : ["学习一个知识点", "完成一项工作", "复盘今日收获"];
        var result = new List<ChecklistItem>();
        foreach (string line in titles)
        {
            string title = line.Trim();
            if (title.Length == 0) continue;
            if (title.Length > 80) throw new InvalidDataException("tasks.txt 中的任务名称不能超过 80 个字符。");
            result.Add(new ChecklistItem { Title = title });
        }
        return result;
    }
}
