using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Conesoft.Files;

sealed partial class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    SafeFindHandle() : base(true) { }
    protected override bool ReleaseHandle()
    {
        return FindClose(handle);
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FindClose(nint handle);
}

