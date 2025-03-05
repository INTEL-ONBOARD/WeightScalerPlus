using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using System.Xml.Linq;

namespace WeightMaster.Interfaces.UserControls
{
    /// <summary>
    /// Interaction logic for Station1TableRow.xaml
    /// </summary>
    public partial class Station1TableRow : UserControl
    {
        public Station1TableRow(string roundNo, string sacksCount, string boxCount, string acceptedWeight, string goldLeafWeight, string generalLeafWeight)
        {
            InitializeComponent();

            // Assign values to the TextBoxes
            this.roundNo.Text = roundNo;
            this.sacksCount.Text = sacksCount;
            this.boxCount.Text = boxCount;
            this.acceptedWeight.Text = acceptedWeight;
            this.goldLeafWeight.Text = goldLeafWeight;
            this.generalLeafWeight.Text = generalLeafWeight;
        }
    }
}
