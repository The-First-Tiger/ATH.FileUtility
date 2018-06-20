namespace ATH.FileUtility.Core.Model
{
    using System;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;

    internal static class Win32FileSize
    {
        private const uint GenericRead = 0x80000000;
        private const uint FileShareRead = 1;
        private const uint FileShareWrite = 2;
        private const uint OpenExisting = 3;
        private const uint FileAttributeNormal = 0x80;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr securityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool GetFileSizeEx(IntPtr hFile, out long lpFileSize);

        internal static long Get(string filepath)
        {
            var handle = CreateFile(
                                filepath,
                                GenericRead,
                                FileShareRead | FileShareWrite,
                                IntPtr.Zero,
                                OpenExisting,
                                FileAttributeNormal,
                                IntPtr.Zero);

            GetFileSizeEx(handle, out var size);

            CloseHandle(handle);

            return size;
        }
    }
}