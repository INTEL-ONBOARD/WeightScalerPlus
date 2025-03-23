using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WeightMaster.Interfaces.UserControls
{
    public partial class CustomerCompletionTableRow : UserControl
    {
        public CustomerCompletionTableRow(string memberNo, string name, string sackCount, string remainingSackCount, string totalLeafWeight, string acceptedLeafWeight, string goldLeafWeight)
        {
            InitializeComponent();

            // Assign values to the TextBoxes
            this.memberNo.Text = memberNo;
            this.name.Text = name;
            this.sackCount.Text = sackCount;
            this.remainingSackCount.Text = remainingSackCount;
            this.totalLeafWeight.Text = totalLeafWeight;
            this.acceptedLeafWeight.Text = acceptedLeafWeight;
            this.goldLeafWeight.Text = goldLeafWeight;

            // Convert sackCount & remainingSackCount to integers and update background
            setBackgroundColor(Convert.ToInt32(sackCount), Convert.ToInt32(remainingSackCount));
        }

        private void setBackgroundColor(int sackCount, int remainingSackCount)
        {
            BrushConverter brushConverter = new BrushConverter();

            if (sackCount == remainingSackCount)
            {
                RowBorder.Background = (Brush)brushConverter.ConvertFrom("#FFE9E9"); // Light Red
            }
            else if (remainingSackCount == 0)
            {
                RowBorder.Background = (Brush)brushConverter.ConvertFrom("#D8FFD3"); // Light Green
            }
            else
            {
                RowBorder.Background = (Brush)brushConverter.ConvertFrom("#FFF5D6"); // Light Yellow
            }
        }

        private void RowButton_st2_Click(object sender, RoutedEventArgs e)
        {
            // Handle the click event here
            MessageBox.Show("Row clicked!");
        }
    }
}