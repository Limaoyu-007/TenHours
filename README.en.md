# 10 Hours — Freedom, with Boundaries

[简体中文](README.md) · [English](README.en.md)

> **Freedom is not simply doing whatever you want. It is having the power to choose, and still choosing the commitments you believe in.**

A small, always-on-top Windows desktop timer built around one personal commitment: **spend 10 hours each day on meaningful work and study. Red means the goal is still ahead. Green means the commitment has been met.**

<p align="center">
  <img src="docs/images/desktop-work-study.png" width="280" alt="A real desktop session: 2 hours 21 minutes of work, 1 hour 24 minutes of study, and 3 hours 45 minutes in total" />
</p>

<p align="center">
  <a href="https://github.com/Limaoyu-007/TenHours/releases/latest">Download for Windows</a> ·
  <a href="#screenshots">Screenshots</a> ·
  <a href="#build-from-source">Build from source</a>
</p>

Native **C# / .NET 10 / WinForms**. Local data. No account or network connection required. The application interface is currently in **Simplified Chinese**; this README provides an English introduction and usage guide.

## Why I built this

I have more control over my time than many students. I can decide when to study, when to develop a project, and when to call it a day.

But freedom has another side.

Sometimes, after a few hours, I would think, “I have done pretty well today,” and stop—even though there was still plenty of time available.

The problem was not always a lack of time. **When stopping is always an option, it is easy to feel satisfied too early.**

I did not want to hand control of my time back to someone else. I also did not want the absence of external demands to make my days lose their structure.

So I gave my freedom a rule:

**Each day, accumulate 10 hours of work and study.**

I have the freedom to choose what to do. I also want to take responsibility for giving sustained attention to what I consider important.

## Freedom, with boundaries

Choosing a boundary does not mean giving up the ability to choose. It means **having that ability first, then deciding how to use it**.

I could work for only a few hours. I could stop whenever I wanted. Instead, I choose ten hours as my daily standard of commitment.

Those hours do not have to belong to a single activity. They might include developing a project, learning a technology, investigating a problem, reading documentation, practicing algorithms, or other work and study that moves a meaningful goal forward.

**The point is to use ten hours with care, rather than merely wait for them to pass.**

The timer is a tool, not the purpose. It records real effort; a pleasing number alone does not make a day productive.

## The commitments I make to myself

1. **Ten hours in total each day.** Work and study contribute to the same daily goal.
2. **Count meaningful effort.** Meals, entertainment, and ordinary breaks stay outside the timer.
3. **Finishing one task does not end the day automatically.** There may be another useful problem to investigate or something worth learning.
4. **Ten hours is a threshold, not a cap.** After reaching it, I can continue or choose to finish for the day.
5. **The rule is my own choice.** It helps me keep a commitment; the number does not replace my judgment.

I once had to work toward the freedom to decide how to spend my time. Now I am learning how to set boundaries after gaining that freedom.

Red: keep putting in meaningful effort. Green: the commitment has been fulfilled.

**Freedom is my right. Ten hours is my choice.**

## Work and study, recorded separately

Work and study both matter, but their roles differ:

- **Work** applies skills to an actual problem, a concrete deliverable, or a result that can be used and evaluated.
- **Study** develops knowledge, skills, and methods that make future work possible.

Visible buttons show each category's accumulated time, while a separate total shows progress toward the daily goal. The selected category has a check mark and a distinct color: blue for work, purple for study.

**Work + Study = 10 hours. There are not two separate ten-hour targets.** Only the selected category accrues time. You choose the category yourself; the app does not monitor your activity or decide whether an interval qualifies as productive.

## Screenshots

### A real session and the green-light goal

<p align="center">
  <img src="docs/images/desktop-work-study.png" width="260" alt="Real usage: separate work and study counters, a combined total, and a simple checklist" />
  <img src="docs/images/goal-reached.png" width="260" alt="Demo data: seven hours of work plus three hours of study reaches the goal and turns the light green" />
</p>

The left image is an actual usage screenshot supplied by the author. The right image uses demo data in the native application.

### A separate window for task management

<p align="center">
  <img src="docs/images/task-management.png" width="700" alt="Task manager showing task names, daily or regular task types, completion status, and add, save, delete, and reorder controls" />
</p>

Add, rename, delete, and reorder tasks. Mark a task as daily when it should return unchecked each new day. The checklist is a reminder, with no time estimates or scheduling system.

### Daily time and completed tasks

<p align="center">
  <img src="docs/images/daily-history.png" width="760" alt="Daily history with separate work and study durations, a combined total, goal status, and a list of completed tasks for the selected day" />
</p>

The task manager, history, and green-light screenshots use isolated demo data in actual WinForms windows. They do not expose the author's saved history.

## Download and run

Download [Windows x64 · v1.5.1](https://github.com/Limaoyu-007/TenHours/releases/download/v1.5.1/TenHours-v1.5.1-win-x64.zip), or visit [the latest release](https://github.com/Limaoyu-007/TenHours/releases/latest). Extract the ZIP and double-click `TenHours.exe`.

- Windows 10/11 x64.
- The release includes the .NET runtime; no separate runtime installation is needed.
- The app starts paused. Start with Windows is off by default.
- To upgrade, choose **退出** (Exit) from the old instance's tray menu, then launch the new version. Your local history and tasks remain available.

## Everyday use

| Chinese label | Meaning |
| --- | --- |
| 工作 / 学习 | Work / Study: select the active category |
| 开始工作 / 开始学习 | Start the selected category |
| 暂停工作 / 暂停学习 | Pause the selected category |
| 今日总计 | Today's combined total |
| 任务清单 / 管理任务 | Checklist / Manage tasks |
| 每日任务 | Daily task: resets to unchecked each new day |
| 查看历史 / 补录时间 | History / Add a past time interval |
| 退出 | Exit and stop timing |

Select **工作** or **学习**, then press Start. Switching categories while running settles the previous category and continues in the new one. Switching while paused keeps the timer paused.

Drag the window background to move it. The widget stays on top, remembers its position, and does not occupy a taskbar slot. Hiding it, including with Alt+F4, keeps the timer running; use the tray menu's Exit command to stop the application. Double-click the tray icon to bring it back.

Click the checklist heading to collapse or expand the task area. The adjacent **···** menu can hide completed tasks or put daily tasks first. These preferences persist. Daily-first sorting affects the widget without changing the order saved in the task manager.

The current preset contains 15 editable work and study tasks. Daily tasks reset at midnight, including when reopening the app on a later day. Regular tasks keep their checked state across days. Tasks are independent of the timer.

History shows work, study, and total time for each date. Select a date to see its completed tasks. Renaming or deleting a task does not rewrite an earlier completion record. Unchecking a task removes its entry for the current day only. A regular task left checked is not counted again every following day.

Task completion history begins with version 1.4.0. Older completion dates cannot be reconstructed; those dates are marked as unrecorded. Existing time history and task states are preserved.

## Manual time entries

Choose a date, start and end times, and either work or study. Entries are **additive**, including overlaps and duplicates: if today already contains two hours, adding `09:30–15:30` brings the total to eight hours.

For an interval crossing midnight, check the next-day option; the duration is split between dates. Each entry can cover up to 24 hours and must end in the past. Adding an entry preserves the active category and running or paused state.

## Local data and timing

All user data stays in `%LOCALAPPDATA%\TenHours\`, independently of where the executable is stored.

| File | Purpose |
| --- | --- |
| `data.json` | Time history, completed tasks, checklist settings, position, mode, and manual entries |
| `data.json.bak` | The previous complete saved copy |
| `data.damaged-*.json` | A damaged primary file retained during backup recovery |

Pause and exit before copying this folder for backup. There is no automatic history deletion.

Elapsed time comes from a monotonic clock rather than counting UI timer ticks. The app pauses on Windows lock or suspend notifications; resume manually after unlocking or waking. Normal reopening starts paused and does not count offline time.

Running time is checkpointed approximately every five seconds. Pausing, switching categories, manual entries, task changes, date changes, and exit trigger saves. Data is written to a temporary file, flushed, and atomically moved or replaced, with a previous-copy backup. A save failure pauses timing and retains the in-memory progress for retry.

A crash or power loss can lose time since the last checkpoint—normally about five seconds, potentially more if the UI is blocked. If the primary file is damaged, the app tries its backup. If neither is readable, it preserves both and refuses to overwrite them.

## Build from source

Use Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Visual Studio is optional. The application uses no third-party business libraries.

```powershell
git clone https://github.com/Limaoyu-007/TenHours.git
cd TenHours
dotnet build .\TenHours.slnx -c Release
dotnet run --project .\src\TenHours.App\TenHours.App.csproj -c Release
```

Publish a self-contained single-file executable:

```powershell
.\scripts\Publish.ps1
# Optional ARM64 build; verify it on the target device separately.
.\scripts\Publish.ps1 -Runtime win-arm64
```

The x64 executable is written to `artifacts/win-x64/TenHours.exe`. Bundling the runtime increases the download size; file size is not the same as resident memory use. Native libraries may be extracted to the .NET cache on first launch. WinForms trimming is not enabled.

## Project layout

```text
src/TenHours.Core/       Timing, daily records, checklist data, atomic JSON persistence
src/TenHours.App/        Native widget, task manager, history, tray, manual entries
tests/TenHours.Tests/   Core behavior regression program
scripts/Publish.ps1      Single-file publishing
scripts/Verify.ps1       Optional core and native-window verification
docs/images/           Actual usage and native-window demo screenshots
```

The core regression program covers timing, midnight boundaries, manual entries, recovery, and task persistence. Version 1.4.0 passed 25 core cases. Documentation screenshots show real windows but do not establish full interaction or long-running performance verification. See [verification details and limits, in Chinese](docs/VALIDATION.md).

## Releases

- **1.5.1:** Chinese and English project documentation, real usage and native-window screenshots. Application features carry over from 1.5.0.
- **1.5.0:** Visible work/study selectors, separate counters, a combined total, and explicit category labels.
- **1.4.0:** Checklist display controls and daily task completion history.
- **1.3.0:** A separate task manager and daily recurring tasks.
- **1.2.1:** A dark scrollbar and a preset checklist.
- **1.2.0:** A persistent checklist below the timer.
- **1.1.0:** Manual time entries.
- **1.0.0:** The ten-hour timer, status light, tray, and local history.
