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
                                // Start a background task to repeatedly update the value
            Task.Run(() =>
            {
                while (true) // You can replace this with a more controlled loop if needed
                {
                    // Dispatch the UI update to the main thread
                    Dispatcher.Invoke(() =>
                    {


                        if (_mainWindow.Station1Frame.IsVisible)
                        {
                            scalerWeightLbl_cust.Text = _mainWindow.weightScalerValTxt_st1.Text;
                            if (_mainWindow.weightScalerStatus_st1.Text.Equals("සමබරයි"))
                            {
                                // Update color for Station 1
                                var brushConverter = new BrushConverter();
                                OutColor.Fill = (Brush)brushConverter.ConvertFromString("#2ECC71"); // Example: Green
                            }
                            else 
                            {
                                // Update color for Station 1
                                var brushConverter = new BrushConverter();
                                OutColor.Fill = (Brush)brushConverter.ConvertFromString("#E74C3C"); // Example: Red
                            }

                            //acceptedWeightLbl_cust.Text = _mainWindow.acceptedLeafWeightTxt_st1.Text;
                            if (_mainWindow.acceptedLeafWeightTxt_st1.Text.Equals(""))
                            {
                                acceptedWeightLbl_cust.Text = "0";
                            }
                            else 
                            {
                                acceptedWeightLbl_cust.Text = _mainWindow.acceptedLeafWeightTxt_st1.Text;
                            }

                            //sacks and boxes
                            if (_mainWindow.nSacksTxt_st1.Text.Equals(""))
                            {
                                nSacks_cust.Text = "0";
                            }
                            else
                            {
                                nSacks_cust.Text = _mainWindow.nSacksTxt_st1.Text;
                            }

                            if (_mainWindow.nBoxesTxt_st1.Text.Equals(""))
                            {
                                nBoxes_cust.Text = "0";
                            }
                            else
                            {
                                nBoxes_cust.Text = _mainWindow.nBoxesTxt_st1.Text;
                            }

                            //deductions
                            if (_mainWindow.wateredTxt_st1.Text.Equals(""))
                            {
                                watered_cust.Text = "0";
                            }
                            else
                            {
                                watered_cust.Text = _mainWindow.wateredTxt_st1.Text;
                            }

                            if (_mainWindow.spoiledTxt_st1.Text.Equals(""))
                            {
                                spoiled_cust.Text = "0";
                            }
                            else
                            {
                                spoiled_cust.Text = _mainWindow.spoiledTxt_st1.Text;
                            }

                            if (_mainWindow.maturedTxt_st1.Text.Equals(""))
                            {
                                matured_cust.Text = "0";
                            }
                            else
                            {
                                matured_cust.Text = _mainWindow.maturedTxt_st1.Text;
                            }

                            if (_mainWindow.rejectedTxt_st1.Text.Equals(""))
                            {
                                rejected_cust.Text = "0";
                            }
                            else
                            {
                                rejected_cust.Text = _mainWindow.rejectedTxt_st1.Text;
                            }

                            //total sack & box weights
                            //totalBoxWeight_cust
                            if (_mainWindow.nBoxesTxt_st1.Text.Equals(""))
                            {
                                totalBoxWeight_cust.Text = "0";
                            }
                            else
                            {
                                if (!double.TryParse(_mainWindow.nBoxesTxt_st1.Text, out double nBoxes) || nBoxes < 0)
                                    nBoxes = 0;

                                double boxWeights = Math.Ceiling(nBoxes * 3.5);
                                totalBoxWeight_cust.Text = _mainWindow.rejectedTxt_st1.Text;
                            }

                            //member deteails
                            if (_mainWindow.barcodeTxt_st1.Text.Equals(""))
                            {
                                barcode_cust.Text = "-";
                            }
                            else
                            {
                                barcode_cust.Text = _mainWindow.barcodeTxt_st1.Text;
                            }
                            if (_mainWindow.customerNameTxt_st1.Text.Equals("") || _mainWindow.customerNameTxt_st1.Text.Equals("No name with initials found"))
                            {
                                customer_cust.Text = "-";
                            }
                            else
                            {
                                customer_cust.Text = _mainWindow.customerNameTxt_st1.Text;
                            }

                            if (_mainWindow.lineNameCmb_st1.SelectedValue != null)
                            {
                               route_cust.Text  = _mainWindow.lineNameCmb_st1.SelectedValue.ToString();
                            }
                            else
                            {
                                // Handle the case when no item is selected.
                                // For example, assign a default value or display an error message.
                                route_cust.Text = ""; // or any appropriate default
                            }
                            //
                            if (_mainWindow.customerNameTxt_st1.Text.Equals(""))
                            {
                                agent_cust.Text = "-";
                            }
                            else
                            {
                                agent_cust.Text = _mainWindow.lineMasterNameLbl_st1.Text;
                            }

                        }

                        //station 2___________________________________________________________________________________
                        else if (_mainWindow.Station2Frame.IsVisible)
                        {
                            scalerWeightLbl_cust.Text = _mainWindow.weightScalerValTxt_st2.Text;
                            if (_mainWindow.weightScalerStatus_st2.Text.Equals("සමබරයි"))
                            {
                                // Update color for Station 1
                                var brushConverter = new BrushConverter();
                                OutColor.Fill = (Brush)brushConverter.ConvertFromString("#2ECC71"); // Example: Green
                            }
                            else
                            {
                                // Update color for Station 1
                                var brushConverter = new BrushConverter();
                                OutColor.Fill = (Brush)brushConverter.ConvertFromString("#E74C3C"); // Example: Red
                            }

                            acceptedWeightLbl_cust.Text = _mainWindow.acceptedSackWeightTxt_st2.Text;
                            if (_mainWindow.totalNSacksTxt_st2.Text.Equals(""))
                            {
                                acceptedWeightLbl_cust.Text = "0";
                            }
                            else
                            {
                                acceptedWeightLbl_cust.Text = _mainWindow.acceptedSackWeightTxt_st2.Text;
                            }


                            //if (_mainWindow.nBoxesTxt_st2.Text.Equals(""))
                            //{
                            //    nBoxes_cust.Text = "0";
                            //}
                            //else
                            //{
                            //    nBoxes_cust.Text = _mainWindow.nBoxesTxt_st2.Text;
                            //}
                            //deductions
                            if (_mainWindow.wateredTxt_st2.Text.Equals(""))
                            {
                                watered_cust.Text = "0";
                            }
                            else
                            {
                                watered_cust.Text = _mainWindow.wateredTxt_st2.Text;
                            }

                            if (_mainWindow.spoiledTxt_st2.Text.Equals(""))
                            {
                                spoiled_cust.Text = "0";
                            }
                            else
                            {
                                spoiled_cust.Text = _mainWindow.spoiledTxt_st2.Text;
                            }

                            if (_mainWindow.maturedTxt_st2.Text.Equals(""))
                            {
                                matured_cust.Text = "0";
                            }
                            else
                            {
                                matured_cust.Text = _mainWindow.maturedTxt_st2.Text;
                            }

                            if (_mainWindow.rejectedTxt_st2.Text.Equals(""))
                            {
                                rejected_cust.Text = "0";
                            }
                            else
                            {
                                rejected_cust.Text = _mainWindow.rejectedTxt_st2.Text;
                            }

                            //total sack & box weights
                            //totalBoxWeight_cust
                            //if (_mainWindow.nBoxesTxt_st2.Text.Equals(""))
                            //{
                            //    totalBoxWeight_cust.Text = "0";
                            //}
                            //else
                            //{
                            //    if (!double.TryParse(_mainWindow.nBoxesTxt_st2.Text, out double nBoxes) || nBoxes < 0)
                            //        nBoxes = 0;

                            //    double boxWeights = Math.Ceiling(nBoxes * 3.5);
                            //    totalBoxWeight_cust.Text = _mainWindow.rejectedTxt_st2.Text;
                            //}

                            //member deteails
                            if (_mainWindow.barcodeTxt_st2.Text.Equals(""))
                            {
                                barcode_cust.Text = "-";
                            }
                            else
                            {
                                barcode_cust.Text = _mainWindow.barcodeTxt_st2.Text;
                            }
                            if (_mainWindow.customerNameTxt_st2.Text.Equals("") || _mainWindow.customerNameTxt_st2.Text.Equals("No name with initials found"))
                            {
                                customer_cust.Text = "-";
                            }
                            else
                            {
                                customer_cust.Text = _mainWindow.customerNameTxt_st2.Text;
                            }

                            if (_mainWindow.lineNameCmb_st2.SelectedValue != null)
                            {
                                route_cust.Text = _mainWindow.lineNameCmb_st2.SelectedValue.ToString();
                            }
                            else
                            {
                                // Handle the case when no item is selected.
                                // For example, assign a default value or display an error message.
                                route_cust.Text = ""; // or any appropriate default
                            }
                            //
                            if (_mainWindow.customerNameTxt_st2.Text.Equals(""))
                            {
                                agent_cust.Text = "-";
                            }
                            else
                            {
                                agent_cust.Text = _mainWindow.lineMasterNameLbl_st2.Text;
                            }

                        }









                    });

                    // Pause for 1 second between updates (adjust as needed)
                    Thread.Sleep(1000);
                }
            });


        }

        //private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    //if (e.PropertyName == nameof(MainWindow.YourProperty))
        //    //{
        //    //    // Update UI or logic here when "YourProperty" changes
        //    //    Dispatcher.Invoke(() =>
        //    //    {
        //    //        //YourTextBox.Text = _mainWindow.YourProperty; // Example update
        //    //    });
        //    //}
        //}

        private void exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
