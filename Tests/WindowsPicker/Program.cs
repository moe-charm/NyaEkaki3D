using System.Runtime.InteropServices;
using System.Text;
using Viewer.Runtime;

internal static class Program
{
    delegate bool EnumWindow(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, EnumWindow callback, IntPtr parameter);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr window, StringBuilder name, int size);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] static extern int GetDlgCtrlID(IntPtr window);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)] static extern IntPtr SendText(IntPtr window, uint message, IntPtr wParam, string text);

    static IntPtr OwnDialog()
    {
        IntPtr result = IntPtr.Zero;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out uint process);
            var name = new StringBuilder(128); GetClassName(window, name, name.Capacity);
            if (process == Environment.ProcessId && name.ToString() == "#32770") { result = window; return false; }
            return true;
        }, IntPtr.Zero);
        return result;
    }

    static string Exercise(string directory, string selection)
    {
        var task = WindowsFilePicker.Open(IntPtr.Zero, directory);
        var limit = DateTime.UtcNow.AddSeconds(20);
        IntPtr dialog = IntPtr.Zero;
        while (dialog == IntPtr.Zero && !task.IsCompleted && DateTime.UtcNow < limit) { dialog = OwnDialog(); Thread.Sleep(50); }
        if (dialog == IntPtr.Zero) throw new Exception("Native dialog was not created: " + task.Exception);
        // Interact only with this test process's dialog; keep the test unobtrusive.
        ShowWindow(dialog, 0);
        Thread.Sleep(400);
        if (selection != null)
        {
            // Select the filename edit by ID; the dialog also contains a search edit.
            IntPtr edit = IntPtr.Zero;
            EnumChildWindows(dialog, (child, _) =>
            {
                var name = new StringBuilder(128); GetClassName(child, name, name.Capacity);
                if (name.ToString() == "Edit" && GetDlgCtrlID(child) == 0x047c) edit = child;
                return true;
            }, IntPtr.Zero);
            if (edit == IntPtr.Zero) throw new Exception("Filename edit not found");
            SendText(edit, 0x000c, IntPtr.Zero, selection);
            PostMessage(dialog, 0x0111, (IntPtr)1, IntPtr.Zero);
        }
        else PostMessage(dialog, 0x0111, (IntPtr)2, IntPtr.Zero);
        if (!task.Wait(TimeSpan.FromSeconds(15)))
        {
            PostMessage(dialog, 0x0010, IntPtr.Zero, IntPtr.Zero);
            throw new Exception("Native dialog did not complete");
        }
        return task.Result;
    }

    static int Main()
    {
        var directory = Path.Combine(Path.GetTempPath(), "NyaForge-Picker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var selectedFile = Path.Combine(directory, "fixture's 日本語.json");
        File.WriteAllText(selectedFile, "{}");
        try
        {
            if (Exercise(directory, null) != null) throw new Exception("Cancel returned a filename");
            Console.WriteLine("PASS native cancel");
            var selected = Exercise(directory, selectedFile);
            if (selected != selectedFile) throw new Exception("Unicode/spaced path differs: " + selected);
            Console.WriteLine("PASS native file selection with Unicode, spaces and apostrophe");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
