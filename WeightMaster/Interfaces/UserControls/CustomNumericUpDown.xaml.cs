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

            // Handle paste operations to ensure only numbers are pasted
            DataObject.AddPastingHandler(txtValue, OnPaste);
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

        // Event handler to restrict non-numeric input
        private void txtValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        // Check if the input text is numeric
        private bool IsTextNumeric(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        // Handle paste event to allow only numeric text
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextNumeric(pastedText))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Handle the TextChanged event to update Value
        private void txtValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Try parsing the text; if valid, update Value.
            if (int.TryParse(txtValue.Text, out int newValue))
            {
                // Optionally, enforce minimum and maximum boundaries.
                if (newValue < Minimum)
                    newValue = Minimum;
                else if (newValue > Maximum)
                    newValue = Maximum;

                Value = newValue;
            }
        }
    }
}
