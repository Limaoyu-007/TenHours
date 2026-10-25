using TenHours.Core;

namespace TenHours;

internal static class TaskDefaults
{
    private static readonly string[] Titles =
    [
        "英语单词", "LeetCode 算法", "Java 八股", "后端技术深入", "技术实战与源码",
        "项目二：智能校车", "项目三：校园一张表", "项目四：智能通知", "项目一：统一预约 Agent",
        "比赛路演准备", "比赛提交材料", "个人网站", "LabMS / MeetingMS", "二级单位考核平台", "前端能力补齐"
    ];

    internal static List<ChecklistItem> Create() => Titles.Select(title => new ChecklistItem { Title = title }).ToList();

    internal static bool Initialize(TimerData data)
    {
        if (data.PersonalChecklistInitialized) return false;
        data.Tasks ??= [];
        // Replace old sample rows, retaining real tasks, their IDs and checked state.
        data.Tasks.RemoveAll(task => task.Title is "学习一个知识点" or "完成一项工作" or "复盘今日收获");
        foreach (string title in Titles)
            if (!data.Tasks.Any(task => task.Title == title)) data.Tasks.Add(new ChecklistItem { Title = title });
        data.PersonalChecklistInitialized = true;
        return true;
    }
}
