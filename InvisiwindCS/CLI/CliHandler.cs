using System;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using InvisiwindCS.Services;

namespace InvisiwindCS.CLI
{
    public static class CliHandler
    {
        public static void Run(string[] args)
        {
            AttachParentConsole();

            var hideOption = new Option<bool>(
    aliases: new[] { "--hide", "-h" },
    description: "Hide a window");

            var unhideOption = new Option<bool>(
                aliases: new[] { "--unhide", "-u" },
                description: "Stop hiding a window");

            var targets = new Argument<string[]>(
                name: "targets",
                description: "Process name(s) or PID(s)");

            var rootCmd = new RootCommand("Hide certain windows when sharing your screen")
            {
                hideOption, unhideOption, targets
            };
            rootCmd.Name = "Invisiwind";

            rootCmd.SetHandler((bool hide, bool unhide, string[] tgts) =>
            {
                if (!hide && !unhide)
                {
                    Console.Error.WriteLine("Error: Must specify --hide or --unhide");
                    return;
                }

                var windowsByPid = WindowEnumerator.GetTopLevelWindows()
                    .GroupBy(w => w.Pid)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var target in tgts)
                {
                    if (uint.TryParse(target, out uint pid))
                    {
                        ApplyToPid(pid, hide, windowsByPid);
                    }
                    else
                    {
                        var procs = Process.GetProcessesByName(target.Replace(".exe", ""));
                        if (procs.Length == 0)
                        {
                            Console.Error.WriteLine($"Error: No processes found with name '{target}'");
                            continue;
                        }
                        foreach (var p in procs)
                            ApplyToPid((uint)p.Id, hide, windowsByPid);
                    }
                }
            }, hideOption, unhideOption, targets);

            rootCmd.Invoke(args);
        }

        static void ApplyToPid(uint pid, bool hide,
            System.Collections.Generic.Dictionary<uint,
            System.Collections.Generic.List<Models.WindowInfo>> windowsByPid)
        {
            if (!windowsByPid.TryGetValue(pid, out var wins))
            {
                Console.Error.WriteLine($"Error: No top-level windows for pid {pid}");
                return;
            }
            foreach (var w in wins)
            {
                LoggerService.Info($"CLI: {(hide ? "hide" : "unhide")} pid={pid} hwnd=0x{w.Handle:X}");
                WindowInjector.SetWindowProps(pid, w.Handle, hide, null, w.IsX86);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool AttachConsole(uint dwProcessId);
        static void AttachParentConsole() => AttachConsole(0xFFFFFFFF);
    }
}
