using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using InvisiwindCS.Services;

namespace InvisiwindCS.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly uint _selfPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        public ObservableCollection<WindowViewModel> Windows { get; } = new();

        // ── Preview Frame ───────────────────────────────────────────────
        private BitmapSource? _previewFrame;
        public BitmapSource? PreviewFrame
        {
            get => _previewFrame;
            set { _previewFrame = value; OnPropertyChanged(nameof(PreviewFrame)); }
        }

        // ── Hide From Taskbar ───────────────────────────────────────────
        private bool _hideFromTaskbar;
        public bool HideFromTaskbar
        {
            get => _hideFromTaskbar;
            set
            {
                _hideFromTaskbar = value;
                OnPropertyChanged(nameof(HideFromTaskbar));
                try { HandleHideFromTaskbarChanged(); } catch { }
            }
        }

        // ── Show Desktop Preview ────────────────────────────────────────
        private bool _showDesktopPreview = true;
        public bool ShowDesktopPreview
        {
            get => _showDesktopPreview;
            set
            {
                _showDesktopPreview = value;
                OnPropertyChanged(nameof(ShowDesktopPreview));
                try
                {
                    if (value) _capture.Start();
                    else { _capture.Stop(); PreviewFrame = null; }
                }
                catch (Exception ex) { LoggerService.Error($"Preview toggle: {ex.Message}"); }
            }
        }

        // ── Transparency ────────────────────────────────────────────────
        private bool _enableTransparency;
        public bool EnableTransparency
        {
            get => _enableTransparency;
            set
            {
                _enableTransparency = value;
                OnPropertyChanged(nameof(EnableTransparency));
                try { ApplyTransparencyToHiddenWindows(); } catch { }
            }
        }

        private double _transparencyAmount = 50;
        public double TransparencyAmount
        {
            get => _transparencyAmount;
            set
            {
                _transparencyAmount = value;
                OnPropertyChanged(nameof(TransparencyAmount));
                OnPropertyChanged(nameof(TransparencyPercent));
                if (_enableTransparency)
                    try { ApplyTransparencyToHiddenWindows(); } catch { }
            }
        }

        public int TransparencyPercent => (int)_transparencyAmount;

        void ApplyTransparencyToHiddenWindows()
        {
            foreach (var vm in Windows)
            {
                if (!vm.IsHidden) continue;

                // ✅ Skip our own process
                if (vm.Info.Pid == _selfPid) continue;

                if (_enableTransparency)
                {
                    byte opacity = (byte)(255 - (_transparencyAmount / 100.0 * 255));
                    PeekThroughService.UpdateOpacity(vm.Info.Handle, opacity);
                }
                else
                {
                    PeekThroughService.RestoreOpacity(vm.Info.Handle);
                }
            }
        }


        // ── Status Text ─────────────────────────────────────────────────
        private string _statusText = "Ready";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        // ── Commands ────────────────────────────────────────────────────
        public ICommand RefreshCommand { get; }
        public ICommand UnhideAllCommand { get; }

        // ── Internals ───────────────────────────────────────────────────
        private readonly ScreenCaptureService _capture = new();
        private readonly DispatcherTimer _refreshTimer;
        private readonly Dispatcher _dispatcher;

        // ── Constructor ─────────────────────────────────────────────────
        public MainViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            RefreshCommand = new RelayCommand(_ =>
            {
                try { RefreshWindows(); StatusText = "Refreshed"; }
                catch (Exception ex) { LoggerService.Error($"RefreshCommand: {ex.Message}"); }
            });

            UnhideAllCommand = new RelayCommand(_ =>
            {
                try
                {
                    int count = 0;
                    foreach (var vm in Windows)
                    {
                        if (!vm.IsHidden && !vm.IsClickThrough) continue;

                        if (vm.IsPeeking)
                        {
                            try { PeekThroughService.StopPeek(); } catch { }
                            vm.IsPeeking = false;
                        }

                        // Restore click-through
                        if (vm.IsClickThrough)
                        {
                            try { PeekThroughService.SetClickThrough(vm.Info.Handle, false); } catch { }
                            vm.IsClickThrough = false;
                        }

                        // Restore opacity
                        try { PeekThroughService.RestoreOpacity(vm.Info.Handle); } catch { }

                        if (vm.IsHidden)
                        {
                            vm.IsHidden = false;
                            var captured = vm;
                            Task.Run(() =>
                            {
                                try
                                {
                                    WindowInjector.SetWindowProps(
                                        captured.Info.Pid, captured.Info.Handle,
                                        false, false, captured.Info.IsX86);
                                }
                                catch (Exception ex)
                                {
                                    LoggerService.Error($"UnhideAll item: {ex.Message}");
                                }
                            });
                            count++;
                        }
                    }
                    StatusText = $"Unhid {count} window(s)";
                }
                catch (Exception ex) { LoggerService.Error($"UnhideAllCommand: {ex.Message}"); }
            });

            try { RefreshWindows(); } catch { }

            _capture.FrameArrived += frame =>
            {
                try { _dispatcher.Invoke(() => PreviewFrame = frame); } catch { }
            };

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _refreshTimer.Tick += (_, _) =>
            {
                try { RefreshWindows(); } catch { }
            };
            _refreshTimer.Start();

            try { _capture.Start(); } catch { }
        }

        // ── Refresh Windows List ────────────────────────────────────────
        public void RefreshWindows()
        {
            Task.Run(() =>
            {
                try
                {
                    var current = WindowEnumerator.GetTopLevelWindows();
                    _dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var hiddenHandles = new HashSet<IntPtr>();
                            var peekingHandles = new HashSet<IntPtr>();
                            var clickThruHandles = new HashSet<IntPtr>();

                            foreach (var vm in Windows)
                            {
                                if (vm.IsHidden) hiddenHandles.Add(vm.Info.Handle);
                                if (vm.IsPeeking) peekingHandles.Add(vm.Info.Handle);
                                if (vm.IsClickThrough) clickThruHandles.Add(vm.Info.Handle);
                            }

                            Windows.Clear();
                            foreach (var w in current)
                            {
                                var vm = new WindowViewModel(w, this);
                                if (hiddenHandles.Contains(w.Handle)) vm.IsHidden = true;
                                if (peekingHandles.Contains(w.Handle)) vm.IsPeeking = true;
                                if (clickThruHandles.Contains(w.Handle)) vm.IsClickThrough = true;
                                Windows.Add(vm);
                            }

                            StatusText = $"{Windows.Count} windows · {hiddenHandles.Count} hidden";
                        }
                        catch (Exception ex)
                        {
                            LoggerService.Error($"RefreshWindows UI: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    LoggerService.Error($"RefreshWindows: {ex.Message}");
                }
            });
        }

        // ── Toggle Hide ─────────────────────────────────────────────────
        public void ToggleWindow(WindowViewModel vm)
        {
            try
            {
                if (vm.IsPeeking && !vm.IsHidden)
                {
                    try { PeekThroughService.StopPeek(); } catch { }
                    vm.IsPeeking = false;
                }

                // ✅ Only apply transparency if NOT our own process
                if (vm.Info.Pid != _selfPid)
                {
                    if (vm.IsHidden && _enableTransparency)
                    {
                        byte opacity = (byte)(255 - (_transparencyAmount / 100.0 * 255));
                        try { PeekThroughService.UpdateOpacity(vm.Info.Handle, opacity); } catch { }
                    }

                    if (!vm.IsHidden)
                        try { PeekThroughService.RestoreOpacity(vm.Info.Handle); } catch { }
                }

                bool? taskbar = HideFromTaskbar ? vm.IsHidden : (bool?)null;
                LoggerService.Info(
                    $"Toggle: pid={vm.Info.Pid} hwnd=0x{vm.Info.Handle:X} hide={vm.IsHidden}");

                var captured = vm;
                Task.Run(() =>
                {
                    try
                    {
                        bool ok = WindowInjector.SetWindowProps(
                            captured.Info.Pid, captured.Info.Handle,
                            captured.IsHidden, taskbar, captured.Info.IsX86);

                        _dispatcher.Invoke(() =>
                        {
                            if (!ok)
                            {
                                captured.IsHidden = !captured.IsHidden;
                                StatusText = $"Failed to toggle: {captured.Title}";
                                LoggerService.Error($"Toggle failed pid={captured.Info.Pid}");
                            }
                            else
                            {
                                StatusText = $"{(captured.IsHidden ? "Hidden" : "Shown")}: {captured.Title}";
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        LoggerService.Error($"ToggleWindow task: {ex.Message}");
                        _dispatcher.Invoke(() => captured.IsHidden = !captured.IsHidden);
                    }
                });
            }
            catch (Exception ex)
            {
                LoggerService.Error($"ToggleWindow: {ex.Message}");
            }
        }


        // ── Hide From Taskbar Changed ───────────────────────────────────
        void HandleHideFromTaskbarChanged()
        {
            foreach (var vm in Windows)
            {
                if (!vm.IsHidden) continue;
                var captured = vm;
                Task.Run(() =>
                {
                    try
                    {
                        WindowInjector.SetWindowProps(
                            captured.Info.Pid, captured.Info.Handle,
                            true, HideFromTaskbar, captured.Info.IsX86);
                    }
                    catch (Exception ex)
                    {
                        LoggerService.Error($"HideFromTaskbar: {ex.Message}");
                    }
                });
            }
        }

        // ── Focus / Unfocus ─────────────────────────────────────────────
        public void OnFocused()
        {
            try { RefreshWindows(); } catch { }
            try { if (ShowDesktopPreview) _capture.Start(); } catch { }
        }

        public void OnUnfocused()
        {
            try { _capture.Stop(); } catch { }
        }

        // ── Dispose ─────────────────────────────────────────────────────
        public void Dispose()
        {
            try { _capture.Dispose(); } catch { }
            try { _refreshTimer.Stop(); } catch { }
            try { PeekThroughService.StopPeek(); } catch { }
        }

        // ── INotifyPropertyChanged ──────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
