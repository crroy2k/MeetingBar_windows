using System.Windows;
using System.Windows.Navigation;
using System.Diagnostics;
using MeetingBar.Windows.ViewModels;

namespace MeetingBar.Windows.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel vm) { InitializeComponent(); DataContext = vm; }
        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
        private void OpenLink_Click(object sender, RequestNavigateEventArgs e) { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); e.Handled = true; }
    }
}
