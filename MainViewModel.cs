using System;
using Frontier.Wif.Core.ComponentModel;

namespace TestUIWPF
{
    public class MainViewModel : ViewModelBase
    {
        private DateTime _date = DateTime.Now;
        private DateTime _time = DateTime.Now;

        public DateTime Date
        {
            get => _date;
            set => RaiseAndSetIfChanged(ref _date, value);
        }

        public DateTime Time
        {
            get => _time;
            set => RaiseAndSetIfChanged(ref _time, value);
        }
    }
}