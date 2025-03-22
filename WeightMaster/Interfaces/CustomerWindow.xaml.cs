using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace WeightMaster.Interfaces
{
    /// <summary>
    /// Interaction logic for CustomerWindow.xaml
    /// </summary>
    public partial class CustomerWindow : Window
    {
        private readonly MainWindow _mainWindow; // Reference to MainWindow
        public CustomerWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            DataContext = this; // Optional: Set DataContext for binding
            //_mainWindow.PropertyChanged += MainWindow_PropertyChanged; // Listen for changes
        }

        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == nameof(MainWindow.YourProperty))
            //{
            //    // Update UI or logic here when "YourProperty" changes
            //    Dispatcher.Invoke(() =>
            //    {
            //        //YourTextBox.Text = _mainWindow.YourProperty; // Example update
            //    });
            //}
        }
    }
}
