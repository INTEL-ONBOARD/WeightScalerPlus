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

namespace WeightMaster.Interfaces.UserControls
{
    /// <summary>
    /// Interaction logic for CustomNumericUpDown.xaml
    /// </summary>
    public partial class CustomNumericUpDown : UserControl
    {
        // Public properties for customization
        public int Value { get; set; } = 0;
        public int Minimum { get; set; } = 0;
        public int Maximum { get; set; } = 100;
        public int Increment { get; set; } = 1;

        public CustomNumericUpDown()
        {
            InitializeComponent();
            UpdateText();
        }

        // Update the TextBox with the current value
        private void UpdateText()
        {
            txtValue.Text = Value.ToString();
        }

        // Increment the value if it doesn't exceed Maximum
        private void Increment_Click(object sender, RoutedEventArgs e)
        {
            if (Value + Increment <= Maximum)
            {
                Value += Increment;
                UpdateText();
            }
        }

        // Decrement the value if it doesn't drop below Minimum
        private void Decrement_Click(object sender, RoutedEventArgs e)
        {
            if (Value - Increment >= Minimum)
            {
                Value -= Increment;
                UpdateText();
            }
        }
    }
}
