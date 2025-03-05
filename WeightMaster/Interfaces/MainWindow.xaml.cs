using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WeightMaster.Interfaces;
using WeightMaster.Interfaces.UserControls;
using WeightMaster.Services;

namespace WeightMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Runtime runtimeService;
        private String path;
        public MainWindow()
        {
            InitializeComponent();
            //Topbar
            runtimeService = new Runtime(this , path); // Pass the labels from XAML
            TopBarDate.Text = DateTime.Now.ToString("MM/dd/yyyy");
            OpenCustomerWindow();
        }

        private void OpenCustomerWindow()
        {
            CustomerWindow customerWindow = new CustomerWindow();

            // Get primary screen dimensions
            double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
            double primaryScreenHeight = SystemParameters.PrimaryScreenHeight;

            // Get virtual screen dimensions (total for all monitors)
            double virtualScreenWidth = SystemParameters.VirtualScreenWidth;
            double virtualScreenHeight = SystemParameters.VirtualScreenHeight;

            // Check if there's more than one screen
            if (virtualScreenWidth > primaryScreenWidth || virtualScreenHeight > primaryScreenHeight)
            {
                // Assuming the secondary screen is to the right of the primary screen
                customerWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                customerWindow.Left = primaryScreenWidth; // Position at the start of the second screen
                customerWindow.Top = 0; // Align to the top
            }
            else
            {
                // If no secondary screen, open normally
                customerWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            customerWindow.Show();
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        //Login Frame
        private void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordTextBox.Password;


            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("කරුණාකර නිවැරදි පරිශීලක නාමය හා මුරපදය ඇතුලත් කරන්න", "Validation Error");
                return;
            }
            else if (RadioBtnStation1.IsChecked == false && RadioBtnStation2.IsChecked == false) 
            {
                MessageBox.Show("කරුණාකර ප්‍රවේශ වීමට මැදිරියක් තෝරාගන්න.", "Validation Error");
                return;
            }

            // Check which radio button is selected and show the corresponding page
            else if (RadioBtnStation1.IsChecked == true)
            {
                LoginFrame.Visibility = Visibility.Collapsed;
                StationMainFrame.Visibility = Visibility.Visible;
                Station1Frame.Visibility = Visibility.Visible;

                //load table rows(test)
                Station1TableRow station1TableRow1 = new Station1TableRow("02", "20", "0", "64KG", "20KG", "88KG");
                Station1TableRow station1TableRow2 = new Station1TableRow("02", "20", "0", "64KG", "20KG", "88KG");
                Station1TablePanel.Children.Add(station1TableRow1);
                Station1TablePanel.Children.Add(station1TableRow2);

                //change topbar text
                TopBarText.Text = "වේදිකාව-1";

            }
            else if (RadioBtnStation2.IsChecked == true)
            {
                LoginFrame.Visibility = Visibility.Collapsed;
                StationMainFrame.Visibility = Visibility.Visible;
                Station2Frame.Visibility = Visibility.Visible;

                //load table rows(test)
                CustomerCompletionTableRow cctr1 = new CustomerCompletionTableRow("XLR9590565", "Mr. Kulathunga", "6", "4", "320KG", "320KG", "200KG");
                CustomerCompletionTableRow cctr2 = new CustomerCompletionTableRow("XLR9590565", "Mr. Kulathunga", "6", "6", "320KG", "320KG", "200KG");
                CustomerCompletionTableRow cctr3 = new CustomerCompletionTableRow("XLR9590565", "Mr. Kulathunga", "6", "0", "320KG", "320KG", "200KG");
                CustomerCompletionRowPanel.Children.Add(cctr1);
                CustomerCompletionRowPanel.Children.Add(cctr2);
                CustomerCompletionRowPanel.Children.Add(cctr3);

                //change topbar text
                TopBarText.Text = "වේදිකාව-2";
            }
            else if (RadioBtnAdmin.IsChecked == true)
            {
                //PageAdmin.Visibility = Visibility.Visible;
            }

        }

        private void TogglePasswordVisibilityClick(object sender, RoutedEventArgs e)
        {

        }

        //topbar section
        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsFrame.Visibility = Visibility.Visible;
            Station1Frame.Visibility = Visibility.Collapsed;
            //button visibility logic
            //Console.Beep();
            HomeButton.Visibility = Visibility.Visible;
            SettingsButton.Visibility = Visibility.Collapsed;
        }

        private void HomeButtonClick(object sender, RoutedEventArgs e)
        {
            Station1Frame.Visibility = Visibility.Visible;
            SettingsFrame.Visibility = Visibility.Collapsed;
            //button visibility logic
            //Console.Beep();
            SettingsButton.Visibility = Visibility.Visible;
            HomeButton.Visibility = Visibility.Collapsed;

        }

        private void LogoutButtonClick(object sender, RoutedEventArgs e)
        {
            LoginFrame.Visibility = Visibility.Visible;
            StationMainFrame.Visibility = Visibility.Collapsed;
            Station1Frame.Visibility= Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;
        }

        private void GeneralSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            GeneralSettingsSection.Visibility = Visibility.Visible;
        }


        //settings frame
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // Optionally, set filters (e.g., only text files, images, etc.)
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            // Show the dialog and check if the user selected a file
            if (openFileDialog.ShowDialog() == true)
            {
                // Get the selected file path
                string filePath = openFileDialog.FileName;
                // Use the file path (e.g., display it in a TextBlock)
                path = filePath;
                FilePathTextField.Text = filePath;
                runtimeService.SetFilePath(path); // Pass file path to the runtime service
                runtimeService.StartFileWatcher(); // Start watching the file
                runtimeService.StartTimer();
            }

        }

        protected override void OnClosed(EventArgs e)
        {
            runtimeService.OnWindowClosed(); // Clean up resources when the window is closed
            base.OnClosed(e);
        }

        //event to accept only numbers in the textboxes
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the input text is numeric
            e.Handled = !IsTextNumeric(e.Text);
        }

        private bool IsTextNumeric(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

    }
}