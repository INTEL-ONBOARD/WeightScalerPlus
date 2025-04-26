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
    /// Interaction logic for Station2TableRow.xaml
    /// </summary>
    public partial class Station2TableRow : UserControl
    {
        public Station2TableRow(string roundNo, string sacksWeight, string acceptedWeight, string goldLeafWeight, string greenLeafWeight)
        {
            InitializeComponent();

            // Assign values to the TextBoxes
            this.roundNo.Text = roundNo;
            this.sacksWeight.Text = sacksWeight;
            this.acceptedWeight.Text = acceptedWeight;
            this.goldLeafWeight.Text = goldLeafWeight;
            this.greenLeafWeight.Text = greenLeafWeight;
        }
    }
}
