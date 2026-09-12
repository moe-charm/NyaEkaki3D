using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NyaForge.UnityRuntime.Platform
{
    /// <summary>Windows Explorer-style picker. No Unity API is used on the STA thread.</summary>
    public static class WindowsFilePicker
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        sealed class OpenFileName
        {
            public int size;
            public IntPtr owner, instance;
            public string filter;
            public IntPtr customFilter;
            public int maxCustomFilter, filterIndex = 1;
            public IntPtr file;
            public int maxFile;
            public IntPtr fileTitle;
            public int maxFileTitle;
            public string initialDirectory, title;
            public int flags;
            public short fileOffset, extensionOffset;
            public string defaultExtension;
            public IntPtr customData, hook, templateName, reserved;
            public int reservedValue, flagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetOpenFileNameW([In, Out] OpenFileName value);
        [DllImport("comdlg32.dll")] static extern int CommDlgExtendedError();
        [DllImport("user32.dll")] public static extern IntPtr GetActiveWindow();

        public static Task<string> Open(IntPtr owner, string initialDirectory, string filter, string title, string extension)
        {
            var completion = new TaskCompletionSource<string>();
            var thread = new Thread(() =>
            {
                IntPtr buffer = IntPtr.Zero;
                try
                {
                    buffer = Marshal.AllocHGlobal(32768 * sizeof(char));
                    Marshal.WriteInt16(buffer, 0);
                    var value = new OpenFileName
                    {
                        owner = owner, file = buffer, maxFile = 32768,
                        filter = filter,
                        initialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : null,
                        title = title,
                        // Explorer, existing file/path, hide read-only, request unchanged directory.
                        flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000004 | 0x00000008,
                        defaultExtension = extension
                    };
                    value.size = Marshal.SizeOf(typeof(OpenFileName));
                    if (GetOpenFileNameW(value)) completion.SetResult(Marshal.PtrToStringUni(value.file));
                    else
                    {
                        int error = CommDlgExtendedError();
                        if (error == 0) completion.SetResult(null);
                        else completion.SetException(new IOException("ファイル選択を開けませんでした (0x" + error.ToString("X") + ")"));
                    }
                }
                catch (Exception error) { completion.SetException(error); }
                finally { if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer); }
            }) { IsBackground = true, Name = "NyaForge file picker" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completion.Task;
        }
    }
}
