using System;
using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace NyaForge.UnityRuntime.Platform
{
    /// <summary>Explicit token-user ACL avoids Mono's unimplemented WindowsIdentity.Owner.</summary>
    internal static class WindowsAuthoringPipe
    {
        [StructLayout(LayoutKind.Sequential)] struct SecurityAttributes
        { public int Length;public IntPtr Descriptor;public int Inherit; }
        [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr pointer);
        [DllImport("advapi32.dll",SetLastError=true)] static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
        [DllImport("advapi32.dll",SetLastError=true)] static extern bool GetTokenInformation(IntPtr token,int type,IntPtr buffer,int length,out int needed);
        [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool ConvertSidToStringSidW(IntPtr sid,out IntPtr text);
        [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(string text,uint revision,out IntPtr descriptor,out uint size);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafePipeHandle CreateNamedPipeW(string name,uint mode,uint pipeMode,uint instances,uint output,uint input,uint timeout,ref SecurityAttributes attributes);
        static string UserSid()
        {
            if(!OpenProcessToken(GetCurrentProcess(),8,out var token)) throw new Win32Exception();
            IntPtr buffer=IntPtr.Zero,text=IntPtr.Zero;
            try
            {
                GetTokenInformation(token,1,IntPtr.Zero,0,out int needed);
                if(needed<=0 || needed>65536) throw new Win32Exception();
                buffer=Marshal.AllocHGlobal(needed);
                if(!GetTokenInformation(token,1,buffer,needed,out _) || !ConvertSidToStringSidW(Marshal.ReadIntPtr(buffer),out text)) throw new Win32Exception();
                return Marshal.PtrToStringUni(text);
            }
            finally { if(text!=IntPtr.Zero) LocalFree(text);if(buffer!=IntPtr.Zero) Marshal.FreeHGlobal(buffer);CloseHandle(token); }
        }
        public static NamedPipeServerStream Create(string instance)
        {
            if(!Guid.TryParseExact(instance,"D",out _)) throw new ArgumentException("Invalid instance ID");
            string sid=UserSid();
            if(!ConvertStringSecurityDescriptorToSecurityDescriptorW("O:"+sid+"D:P(A;;GA;;;"+sid+")",1,out var descriptor,out _)) throw new Win32Exception();
            try
            {
                var attributes=new SecurityAttributes { Length=Marshal.SizeOf(typeof(SecurityAttributes)),Descriptor=descriptor };
                // Duplex, overlapped, first instance; byte mode, remote clients rejected.
                var handle=CreateNamedPipeW(@"\\.\pipe\NyaForge.Authoring."+instance,3|0x40000000|0x80000,8,1,4096,4096,0,ref attributes);
                if(handle.IsInvalid) { int error=Marshal.GetLastWin32Error();handle.Dispose();throw new Win32Exception(error); }
                try { return new NamedPipeServerStream(PipeDirection.InOut,true,false,handle); }
                catch { handle.Dispose();throw; }
            }
            finally { LocalFree(descriptor); }
        }
    }
}
