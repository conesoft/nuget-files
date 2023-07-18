using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Conesoft.Files
{
    sealed partial class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        SafeFindHandle() : base(true) { }
        protected override bool ReleaseHandle()
        {
            return FindClose(handle);
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FindClose(IntPtr handle);
    }

    public static class AlternateDataStreams
    {
        const int ERROR_HANDLE_EOF = 38;

        enum StreamInfoLevels
        {
            FindStreamInfoStandard = 0
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class WIN32_FIND_STREAM_DATA
        {
            public long StreamSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)] public string cStreamName = "";
        }


        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFindHandle FindFirstStreamW(string lpFileName, StreamInfoLevels InfoLevel, [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_STREAM_DATA lpFindStreamData, uint dwFlags);

        [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FindNextStreamW(SafeFindHandle hndFindFile, [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_STREAM_DATA lpFindStreamData);

        public static IEnumerable<File> GetStreams(this File file)
        {
            var streams = file.GetStreamsRaw();

            foreach (var stream in streams)
            {
                var streamName = stream.Split(":")[1];
                yield return File.From(file.Path + (streamName != "" ? ":" + streamName : ""));
            }
        }

        public static IEnumerable<string> GetStreamsRaw(this File file) => new FileInfo(file.Path).GetStreamsRaw();

        public static IEnumerable<string> GetStreamsRaw(this FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            WIN32_FIND_STREAM_DATA findStreamData = new();
            SafeFindHandle handle = FindFirstStreamW(file.FullName, StreamInfoLevels.FindStreamInfoStandard, findStreamData, 0);
            if (handle.IsInvalid)
            {
                throw new Win32Exception();
            }
            try
            {
                do
                {
                    yield return findStreamData.cStreamName;
                }
                while (FindNextStreamW(handle, findStreamData));

                int lastError = Marshal.GetLastWin32Error();
                if (lastError != ERROR_HANDLE_EOF)
                {
                    throw new Win32Exception(lastError);
                }
            }
            finally
            {
                handle.Dispose();
            }
        }
    }
}

