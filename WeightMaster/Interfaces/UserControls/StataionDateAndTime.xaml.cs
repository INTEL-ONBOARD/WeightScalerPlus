using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WeightMaster.Interfaces.UserControls
{
    /// <summary>
    /// Interaction logic for StataionDateAndTime.xaml
    /// </summary>
    public partial class StataionDateAndTime : UserControl
    {
        private DispatcherTimer _timer;
        public StataionDateAndTime()
        {
            InitializeComponent();
            UpdateDateTime();
            StartTimer();
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            DateTime now = DateTime.Now;
            DateTextBlock.Text = now.ToString("yyyy - MM - dd");
            TimeTextBlock.Text = now.ToString("hh:mm tt");
        }
    }
}
