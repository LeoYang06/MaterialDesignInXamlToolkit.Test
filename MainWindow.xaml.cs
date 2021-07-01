using System;
using System.Windows;
using Frontier.Wif.Utilities.Extensions;
using MaterialDesignThemes.Wpf;

namespace TestUIWPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _mainViewModel = new MainViewModel();
        }

        public void CombinedDialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        {
            CombinedCalendar.SelectedDate = _mainViewModel.Date;
            CombinedClock.Time = _mainViewModel.Time;
        }

        public void CombinedDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (Convert.ToBoolean(Convert.ToUInt16(eventArgs.Parameter)) && CombinedCalendar.SelectedDate is DateTime selectedDate)
            {
                DateTime combined = selectedDate.AddSeconds(CombinedClock.Time.TimeOfDay.TotalSeconds);
                _mainViewModel.Time = combined;
                _mainViewModel.Date = combined;
            }
        }
    }
}