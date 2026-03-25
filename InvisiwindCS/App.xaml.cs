using System;
using System.Windows;
using System.Windows.Threading;
using InvisiwindCS.CLI;
using InvisiwindCS.Services;

namespace InvisiwindCS
{
    public partial class App : Application
    {
        private GlobalHotkeyService? _hotkeys;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Catch ALL unhandled exceptions — prevent any crash
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                LoggerService.Error($"UnhandledException: {ex.ExceptionObject}");

            DispatcherUnhandledException += (s, ex) =>
            {
                LoggerService.Error($"DispatcherException: {ex.Exception.Message}");
                ex.Handled = true; // Prevent crash
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                LoggerService.Error($"TaskException: {ex.Exception.Message}");
                ex.SetObserved(); // Prevent crash
            };

            LoggerService.Initialize();

            if (e.Args.Length > 0)
            {
                LoggerService.Info($"CLI mode: {string.Join(" ", e.Args)}");
                try { CliHandler.Run(e.Args); } catch (Exception ex) { LoggerService.Error(ex.Message); }
                Shutdown(0);
            }
            else
            {
                LoggerService.Info("GUI mode started");
                try { _hotkeys = new GlobalHotkeyService(LoggerService.Info); }
                catch (Exception ex) { LoggerService.Error($"Hotkey init failed: {ex.Message}"); }

                var window = new MainWindow();
                window.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { _hotkeys?.Dispose(); } catch { }
            try { LoggerService.Dispose(); } catch { }
            base.OnExit(e);
        }
    }
}
