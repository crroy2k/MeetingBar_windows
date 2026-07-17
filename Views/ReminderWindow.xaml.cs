using System;
using System.Windows;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Views
{
    public partial class ReminderWindow : Window
    {
        private readonly Action _join;
        public ReminderWindow(CalendarEvent item, Action join) { InitializeComponent(); DataContext = item; _join = join; }
        private void Join_Click(object sender, RoutedEventArgs e) { _join(); Close(); }
        private void Dismiss_Click(object sender, RoutedEventArgs e) => Close();
    }
}
