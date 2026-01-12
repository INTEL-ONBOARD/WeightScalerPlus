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
    /// Interaction logic for Station1LineTableRow.xaml
    /// </summary>
    public partial class Station1LineTableRow : UserControl
    {
        public Station1LineTableRow(string roundNo, string memberNo, string sacksCount, string boxCount, string goldLeafWeight, string generalLeafWeight, string acceptedWeight)
        {
            InitializeComponent();

            // Assign values to the TextBoxes
            this.roundNo.Text = roundNo;
            this.memberNo.Text = memberNo;
            this.sacksCount.Text = sacksCount;
            this.boxCount.Text = boxCount;
            this.goldLeafWeight.Text = goldLeafWeight;
            this.generalLeafWeight.Text = generalLeafWeight;
            this.acceptedWeight.Text = acceptedWeight;
        }
    }
}
