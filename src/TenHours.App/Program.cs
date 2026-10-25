using System.Security.Principal;
using TenHours.Core;

namespace TenHours;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--preview")
        {
            Verification.Preview(Path.GetFullPath(args[1]));
            return;
        }
        // Verification runs are isolated from real user history and registry settings.
        if (args.Length == 2 && args[0] == "--verify")
        {
            Verification.Run(Path.GetFullPath(args[1]));
            return;
        }

        string user = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        using var singleInstance = new Mutex(true, @"Local\TenHours-" + user, out bool created);
        if (!created)
        {
            NativeMethods.PostMessage(0xffff, NativeMethods.ShowMessage, 0, 0);
            return;
        }
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TenHours");
            var store = new JsonStore(directory);
            var result = store.Load();
            Application.Run(new TimerForm(new TimerEngine(result.Data, new SystemClock()), store, result.Warning));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(ex.Message, "十小时 · 无法读取数据", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { singleInstance.ReleaseMutex(); }
    }
}
