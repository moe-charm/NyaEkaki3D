using System;
using System.Threading.Tasks;

namespace Viewer.Runtime
{
    internal static class WindowsFilePicker
    {
        internal static IntPtr GetActiveWindow()=>NyaForge.UnityRuntime.Platform.WindowsFilePicker.GetActiveWindow();
        public static Task<string> Open(IntPtr owner,string initialDirectory)=>
            NyaForge.UnityRuntime.Platform.WindowsFilePicker.Open(owner,initialDirectory,
                "NyaForge パック・確認セット (*.json)\0*.json\0すべてのファイル (*.*)\0*.*\0\0",
                "NyaForge — パック・確認セットを開く","json");
    }
}
