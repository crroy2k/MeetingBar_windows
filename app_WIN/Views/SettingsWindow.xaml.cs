using System.Windows;
using MeetingBar.Windows.ViewModels;

namespace MeetingBar.Windows.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel vm) { InitializeComponent(); DataContext = vm; }
        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
