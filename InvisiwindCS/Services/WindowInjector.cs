using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using InvisiwindCS.Native;

namespace InvisiwindCS.Services
{
    public static class WindowInjector
    {
        // Track injected processes so we don't inject twice
        private static readonly HashSet<uint> _injectedPids = new();

        static string DllPath(bool isX86) => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            isX86 ? "InvisiwindHook32.dll" : "InvisiwindHook.dll");

        [StructLayout(LayoutKind.Sequential)]
        struct HideParams
        {
            public IntPtr hwnd;
            public int hide; // 1 = hide, 0 = show
        }

        public static bool SetWindowProps(uint pid, IntPtr hwnd, bool hide, bool? hideFromTaskbar, bool isX86)
        {
            try
            {
                string dllPath = DllPath(isX86);
                if (!File.Exists(dllPath))
                {
                    LoggerService.Error($"DLL not found: {dllPath}");
                    return false;
                }

                IntPtr hProcess = Win32.OpenProcess(Win32.PROCESS_ALL_ACCESS, false, pid);
                if (hProcess == IntPtr.Zero)
                {
                    LoggerService.Error($"Cannot open process pid={pid} err={Marshal.GetLastWin32Error()}");
                    return false;
                }

                try
                {
                    // Inject DLL only once per process
                    if (!_injectedPids.Contains(pid))
                    {
                        if (!InjectDll(hProcess, dllPath))
                        {
                            LoggerService.Error($"Injection failed pid={pid}");
                            return false;
                        }
                        _injectedPids.Add(pid);
                        // Wait for DllMain to run
                        System.Threading.Thread.Sleep(200);
                    }

                    // Get remote module base
                    IntPtr remoteBase = GetRemoteModuleBase(pid, Path.GetFileName(dllPath));
                    if (remoteBase == IntPtr.Zero)
                    {
                        LoggerService.Error($"Remote base not found pid={pid}");
                        // Remove from injected list to retry next time
                        _injectedPids.Remove(pid);
                        return false;
                    }

                    // Get local function offsets
                    IntPtr localBase = Win32.LoadLibraryEx(dllPath, IntPtr.Zero, 0x00000001);
                    IntPtr localSetVis = Win32.GetProcAddress(localBase, "SetWindowVisibility");
                    IntPtr localHideTask = Win32.GetProcAddress(localBase, "HideFromTaskbar");
                    Win32.FreeLibrary(localBase);

                    if (localSetVis == IntPtr.Zero)
                    {
                        LoggerService.Error("Export SetWindowVisibility not found");
                        return false;
                    }

                    // Calculate remote addresses
                    long setVisOffset = localSetVis.ToInt64() - localBase.ToInt64();
                    long hideTaskOffset = localHideTask.ToInt64() - localBase.ToInt64();

                    IntPtr remoteSetVis = new IntPtr(remoteBase.ToInt64() + setVisOffset);
                    IntPtr remoteHideTask = new IntPtr(remoteBase.ToInt64() + hideTaskOffset);

                    // Call SetWindowVisibility
                    bool ok = CallRemoteWithParams(hProcess, remoteSetVis,
                        new HideParams { hwnd = hwnd, hide = hide ? 1 : 0 });

                    // Call HideFromTaskbar if needed
                    if (ok && hideFromTaskbar.HasValue && localHideTask != IntPtr.Zero)
                        CallRemoteWithParams(hProcess, remoteHideTask,
                            new HideParams { hwnd = hwnd, hide = hideFromTaskbar.Value ? 1 : 0 });

                    LoggerService.Info($"Success: pid={pid} hwnd=0x{hwnd:X} hide={hide}");
                    return ok;
                }
                finally
                {
                    Win32.CloseHandle(hProcess);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error($"SetWindowProps error: {ex.Message}");
                return false;
            }
        }

        static bool InjectDll(IntPtr hProcess, string dllPath)
        {
            byte[] pathBytes = Encoding.Unicode.GetBytes(dllPath + "\0");
            IntPtr remoteAddr = Win32.VirtualAllocEx(hProcess, IntPtr.Zero,
                (uint)pathBytes.Length, Win32.MEM_COMMIT_RESERVE, Win32.PAGE_READWRITE);
            if (remoteAddr == IntPtr.Zero) return false;

            Win32.WriteProcessMemory(hProcess, remoteAddr, pathBytes, (uint)pathBytes.Length, out _);

            IntPtr loadLib = Win32.GetProcAddress(Win32.GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            IntPtr hThread = Win32.CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLib, remoteAddr, 0, IntPtr.Zero);
            if (hThread == IntPtr.Zero)
            {
                Win32.VirtualFreeEx(hProcess, remoteAddr, 0, Win32.MEM_RELEASE);
                return false;
            }

            Win32.WaitForSingleObject(hThread, 8000);
            Win32.CloseHandle(hThread);
            Win32.VirtualFreeEx(hProcess, remoteAddr, 0, Win32.MEM_RELEASE);
            return true;
        }

        static bool CallRemoteWithParams(IntPtr hProcess, IntPtr remoteFunc, HideParams param)
        {
            // Write HideParams struct into remote process memory
            int structSize = Marshal.SizeOf<HideParams>();
            byte[] paramBytes = new byte[structSize];
            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            try
            {
                Marshal.StructureToPtr(param, ptr, false);
                Marshal.Copy(ptr, paramBytes, 0, structSize);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            IntPtr remoteParam = Win32.VirtualAllocEx(hProcess, IntPtr.Zero,
                (uint)structSize, Win32.MEM_COMMIT_RESERVE, Win32.PAGE_READWRITE);
            if (remoteParam == IntPtr.Zero) return false;

            Win32.WriteProcessMemory(hProcess, remoteParam, paramBytes, (uint)structSize, out _);

            IntPtr hThread = Win32.CreateRemoteThread(hProcess, IntPtr.Zero, 0,
                remoteFunc, remoteParam, 0, IntPtr.Zero);

            if (hThread == IntPtr.Zero)
            {
                Win32.VirtualFreeEx(hProcess, remoteParam, 0, Win32.MEM_RELEASE);
                return false;
            }

            Win32.WaitForSingleObject(hThread, 5000);
            Win32.CloseHandle(hThread);
            Win32.VirtualFreeEx(hProcess, remoteParam, 0, Win32.MEM_RELEASE);
            return true;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr[] lphModule,
            uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll", CharSet = CharSet.Unicode)]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule,
            StringBuilder lpFilename, uint nSize);

        static IntPtr GetRemoteModuleBase(uint pid, string dllFileName)
        {
            string target = dllFileName.ToLowerInvariant();
            IntPtr hProcess = Win32.OpenProcess(0x0410, false, pid);
            if (hProcess == IntPtr.Zero) return IntPtr.Zero;

            try
            {
                IntPtr[] modules = new IntPtr[1024];
                if (!EnumProcessModulesEx(hProcess, modules,
                    (uint)(modules.Length * IntPtr.Size), out uint needed, 0x03))
                    return IntPtr.Zero;

                int count = (int)(needed / IntPtr.Size);
                for (int i = 0; i < count; i++)
                {
                    var sb = new StringBuilder(260);
                    GetModuleFileNameEx(hProcess, modules[i], sb, 260);
                    if (Path.GetFileName(sb.ToString()).ToLowerInvariant() == target)
                        return modules[i];
                }
            }
            finally
            {
                Win32.CloseHandle(hProcess);
            }
            return IntPtr.Zero;
        }
    }
}
