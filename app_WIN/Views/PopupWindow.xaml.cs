using System;
using System.Windows;
using System.Windows.Input;
using MeetingBar.Windows.ViewModels;

namespace MeetingBar.Windows.Views
{
    public partial class PopupWindow : Window
    {
        private readonly Action _openSettings;
        public PopupWindow(MainViewModel viewModel, Action openSettings) { InitializeComponent(); DataContext = viewModel; _openSettings = openSettings; PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) Hide(); }; }
        private void Window_Deactivated(object sender, EventArgs e) { if (IsVisible) Hide(); }
        private void Settings_Click(object sender, RoutedEventArgs e) { Hide(); _openSettings(); }
        private void Quit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}
