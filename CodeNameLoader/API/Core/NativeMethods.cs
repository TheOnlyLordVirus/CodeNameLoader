using System;
using System.Runtime.InteropServices;

namespace CodeNameLoader.API.Core;

static class NativeMethods
{
    public const int PAGE_EXECUTE_READWRITE = 0x40;
    public const int MEM_COMMIT = 0x1000;
    public const int MEM_RESERVE = 0x2000;

    [DllImport("kernelbase.dll")]
    static extern private IntPtr VirtualAllocEx(IntPtr pHandle, IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);

    [DllImport("kernelbase.dll")]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    public delegate int _DllMain(IntPtr image_base, int reason, ulong unk);
}
