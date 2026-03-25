using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InvisiwindCS.ViewModels;

namespace InvisiwindCS
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;
        }

        // ── Focus: show full UI ─────────────────────────────────────────
        private void Window_Activated(object sender, System.EventArgs e)
        {
            try
            {
                FullView.Visibility = Visibility.Visible;
                MiniView.Visibility = Visibility.Collapsed;
                _vm.OnFocused();
            }
            catch { }
        }

        // ── Unfocus: show mini overlay ──────────────────────────────────
        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            try
            {
                FullView.Visibility = Visibility.Collapsed;
                MiniView.Visibility = Visibility.Visible;
                _vm.OnUnfocused();
            }
            catch { }
        }

        // ── Click mini view → restore full UI ──────────────────────────
        private void MiniView_Click(object sender, MouseButtonEventArgs e)
        {
            try { Activate(); } catch { }
        }

        // ── Drag window by title bar ────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                try { DragMove(); } catch { }
        }

        // ── Close button ────────────────────────────────────────────────
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try { _vm.Dispose(); } catch { }
            Close();
        }

        // ── Checkbox changed ────────────────────────────────────────────
        private void WindowCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is CheckBox cb && cb.Tag is WindowViewModel vm)
                    _vm.ToggleWindow(vm);
            }
            catch { }
        }

        // ── On close ────────────────────────────────────────────────────
        protected override void OnClosed(System.EventArgs e)
        {
            try { _vm.Dispose(); } catch { }
            base.OnClosed(e);
        }
    }
}
