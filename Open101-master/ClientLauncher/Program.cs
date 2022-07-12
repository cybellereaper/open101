using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Bleak;

namespace ClientLauncher
{
    public static class Program
    {
        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern IntPtr NtResumeProcess(IntPtr ProcessHandle);

        public static void Main(string[] args)
        {
            const uint CREATE_SUSPENDED = 0x00000004;
            
            const string workingDirectory = @"C:\ProgramData\KingsIsle Entertainment\Wizard101\Bin\";
            string application = $@"{workingDirectory}WizardGraphicalClient.exe";
            const string commandLine = "-L 192.168.0.77 13000 -U .... USER";
            const string dllPath = @"D:\re\wiz101\OpenWizard101\ClassListDumper\bin\Release\ClassListDumper.dll";

            STARTUPINFO sInfo = new STARTUPINFO();
            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);
            tSec.nLength = Marshal.SizeOf(tSec);

            bool retValue = CreateProcess(application, commandLine,
                ref pSec, ref tSec, false, CREATE_SUSPENDED,
                IntPtr.Zero, workingDirectory, ref sInfo, out PROCESS_INFORMATION pInfo);

            Console.WriteLine("Process ID (PID): " + pInfo.dwProcessId);
            Console.WriteLine("Process Handle : " + pInfo.hProcess);
            Process netProcess = Process.GetProcessById(pInfo.dwProcessId);

            Injector d = new Injector();
            d.NtCreateThreadEx(dllPath, netProcess.Id);

            //Console.Out.WriteLine("waiting");
            //Console.In.ReadLine();
            
            NtResumeProcess(pInfo.hProcess);

            netProcess.WaitForExit();
        }
    }
}