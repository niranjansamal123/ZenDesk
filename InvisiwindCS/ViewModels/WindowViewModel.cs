using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using InvisiwindCS.Models;
using InvisiwindCS.Services;

namespace InvisiwindCS.ViewModels
{
    public class WindowViewModel : INotifyPropertyChanged
    {
        public WindowInfo Info { get; }
        public string Title => Info.Title;

        private readonly MainViewModel _parent;

        // ── Icon ────────────────────────────────────────────────────────
        private ImageSource? _icon;
        private bool _iconLoaded;
        public ImageSource? Icon
        {
            get
            {
                if (!_iconLoaded)
                {
                    try { _icon = IconExtractor.GetWindowIcon(Info.Handle); }
                    catch { }
                    _iconLoaded = true;
                }
                return _icon;
            }
        }

        // ── IsHidden ────────────────────────────────────────────────────
        private bool _isHidden;
        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                _isHidden = value;
                OnPropertyChanged(nameof(IsHidden));
                OnPropertyChanged(nameof(CanPeek));

                if (!_isHidden && _isPeeking)
                {
                    try { PeekThroughService.StopPeek(); } catch { }
                    _isPeeking = false;
                    OnPropertyChanged(nameof(IsPeeking));
                    OnPropertyChanged(nameof(PeekLabel));
                }
            }
        }

        // ── IsPeeking ───────────────────────────────────────────────────
        private bool _isPeeking;
        public bool IsPeeking
        {
            get => _isPeeking;
            set
            {
                _isPeeking = value;
                OnPropertyChanged(nameof(IsPeeking));
                OnPropertyChanged(nameof(PeekLabel));
            }
        }

        // ── Peek Opacity ────────────────────────────────────────────────
        private byte _peekOpacity = 128;
        public byte PeekOpacity
        {
            get => _peekOpacity;
            set
            {
                _peekOpacity = value;
                OnPropertyChanged(nameof(PeekOpacity));
                OnPropertyChanged(nameof(PeekOpacityPercent));
                if (_isPeeking)
                {
                    try { PeekThroughService.UpdateOpacity(value); } catch { }
                }
            }
        }

        public int PeekOpacityPercent => (int)Math.Round(_peekOpacity / 255.0 * 100);
        public bool CanPeek => _isHidden;
        public string PeekLabel => _isPeeking ? "👁 Stop Peek" : "👁 Peek";

        // ── IsClickThrough ───────────────────────────────────────────────
        private bool _isClickThrough;
        public bool IsClickThrough
        {
            get => _isClickThrough;
            set
            {
                _isClickThrough = value;
                OnPropertyChanged(nameof(IsClickThrough));

                // ✅ Skip our own process
                if (Info.Pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id)
                    return;

                try
                {
                    PeekThroughService.SetClickThrough(Info.Handle, _isClickThrough);
                    _parent.StatusText = _isClickThrough
                        ? $"Click-through ON: {Title}"
                        : $"Click-through OFF: {Title}";
                }
                catch (Exception ex)
                {
                    LoggerService.Error($"IsClickThrough: {ex.Message}");
                }
            }
        }


        // ── Commands ────────────────────────────────────────────────────
        public ICommand PeekCommand { get; }

        // ── Constructor ─────────────────────────────────────────────────
        public WindowViewModel(WindowInfo info, MainViewModel parent)
        {
            Info = info;
            _parent = parent;
            _isHidden = info.IsHidden;

            PeekCommand = new RelayCommand(_ =>
            {
                try
                {
                    if (!_isPeeking)
                    {
                        if (!_isHidden) return;
                        PeekThroughService.StartPeek(Info.Handle, _peekOpacity);
                        IsPeeking = true;
                        _parent.StatusText = $"Peeking: {Title}";
                    }
                    else
                    {
                        PeekThroughService.StopPeek();
                        IsPeeking = false;
                        _parent.StatusText = "Peek stopped";
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Error($"PeekCommand: {ex.Message}");
                }
            });
        }

        // ── INotifyPropertyChanged ──────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
