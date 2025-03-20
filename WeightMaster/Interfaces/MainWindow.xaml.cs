using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Text.Json;
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
using WeightMaster.Core;
using WeightMaster.Interfaces;
using WeightMaster.Interfaces.UserControls;
using WeightMaster.Services;
using Path = System.IO.Path;

namespace WeightMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Runtime runtimeService;
        private String path;
        private ConsoleHandler _consoleHandler;

        public int nSacks_st1 = 0;
        public int nBoxes_st1 = 0;
        public int singleBoxWeight = 10;

        // Global integer for the total leaf weight(floor value)
        public int totalLeafWeight_st1 = 0;
        public int totalBoxWeight_st1 = 0;






        //private string filePath;
        //private FileSystemWatcher fileWatcher;
        //private DispatcherTimer readTimer;

        //private void BrowseButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Open a file dialog to select the JSON file.
        //    var dialog = new Microsoft.Win32.OpenFileDialog
        //    {
        //        Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        //    };

        //    if (dialog.ShowDialog() == true)
        //    {
        //        filePath = dialog.FileName;
        //        StartFileWatcher();
        //        StartTimer();
        //    }
        //}

        //private void StartFileWatcher()
        //{
        //    // Dispose any existing watcher.
        //    fileWatcher?.Dispose();

        //    // Initialize FileSystemWatcher for the selected file.
        //    fileWatcher = new FileSystemWatcher
        //    {
        //        Path = Path.GetDirectoryName(filePath),
        //        Filter = Path.GetFileName(filePath),
        //        NotifyFilter = NotifyFilters.LastWrite
        //    };

        //    fileWatcher.Changed += OnFileChanged;
        //    fileWatcher.EnableRaisingEvents = true;

        //    // Read the file initially.
        //    ReadFile();
        //}

        //private void StartTimer()
        //{
        //    // Create or restart a DispatcherTimer to refresh the UI continuously.
        //    if (readTimer == null)
        //    {
        //        readTimer = new DispatcherTimer();
        //        readTimer.Interval = TimeSpan.FromMilliseconds(500); // Adjust interval as needed.
        //        readTimer.Tick += (s, e) => ReadFile();
        //    }
        //    readTimer.Start();
        //}

        //private void OnFileChanged(object sender, FileSystemEventArgs e)
        //{
        //    // Use the Dispatcher to ensure the UI is updated on the main thread.
        //    Dispatcher.Invoke(() => ReadFile());
        //}

        //private void ReadFile()
        //{
        //    try
        //    {
        //        // Open the file with sharing enabled to allow concurrent writes.
        //        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //        using (var reader = new StreamReader(stream))
        //        {
        //            string json = reader.ReadToEnd();
        //            // Configure the serializer to ignore case differences.
        //            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        //            var data = JsonSerializer.Deserialize<WeightData>(json, options);
        //            if (data != null)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"> Value: {data.Value}, Stable: {data.Stable}");
        //                // Update the UI with both values.
        //                weightScalerValTxt_st1.Text = data.Value;
        //                weightScalerStatus_st1.Text = data.Stable;
        //            }
        //        }
        //    }
        //    catch (IOException)
        //    {
        //        // If the file is temporarily locked, try again shortly.
        //        Dispatcher.InvokeAsync(() =>
        //        {
        //            System.Threading.Thread.Sleep(100);
        //            ReadFile();
        //        }, DispatcherPriority.Background);
        //        System.Diagnostics.Debug.WriteLine("Issue occured!");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error reading file: {ex.Message}");
        //    }
        //}

        //// Class to represent the JSON structure.
        //public class WeightData
        //{
        //    public string Value { get; set; }
        //    public string Stable { get; set; }
        //}

        //protected override void OnClosed(EventArgs e)
        //{
        //    fileWatcher?.Dispose();
        //    readTimer?.Stop();
        //    runtimeService.OnWindowClosed();
        //    base.OnClosed(e);
        //}





        public MainWindow()
        {
            InitializeComponent();
            //Topbar
            runtimeService = new Runtime(this , path); // Pass the labels from XAML
            TopBarDate.Text = DateTime.Now.ToString("MM/dd/yyyy");
            //OpenCustomerWindow();
            _consoleHandler = new ConsoleHandler();
        }

        //public async Task testExecution()
        //{
        //    System.Diagnostics.Debug.WriteLine("yeees");
        //    await _consoleHandler.GetLineMasterAsync();
        //}

        //global to use on the close event
        private CustomerWindow customerWindow;
        private void OpenCustomerWindow()
        {
            customerWindow = new CustomerWindow();

            // Get primary screen dimensions
            double primaryScreenWidth = SystemParameters.PrimaryScreenWidth;
            double primaryScreenHeight = SystemParameters.PrimaryScreenHeight;

            // Get virtual screen dimensions (total for all monitors)
            double virtualScreenWidth = SystemParameters.VirtualScreenWidth;
            double virtualScreenHeight = SystemParameters.VirtualScreenHeight;

            // Check if there's more than one screen
            if (virtualScreenWidth > primaryScreenWidth || virtualScreenHeight > primaryScreenHeight)
            {
                //binal: in order to work, set the secondory screen to the right of the primary screen(main screen: left, secondory screen: right)
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
            if (customerWindow != null)
            {
                customerWindow.Close();
            }
            this.Close();
        }

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            //await _consoleHandler.GetLineMasterAsync();
            //checks db records 

            await _consoleHandler.VerifyUserDb();
            System.Diagnostics.Debug.WriteLine("> calling start");
            await _consoleHandler.GetLineMasterAsync();
            System.Diagnostics.Debug.WriteLine("> calling done");
            string email = UsernameTextBox.Text;
            string password = PasswordBoxControl.Password;


            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("කරුණාකර නිවැරදි පරිශීලක නාමය හා මුරපදය ඇතුලත් කරන්න", "Validation Error");
                return;
            }
            else if (RadioBtnStation1.IsChecked == false && RadioBtnStation2.IsChecked == false) 
            {
                MessageBox.Show("කරුණාකර ප්‍රවේශ වීමට මැදිරියක් තෝරාගන්න.", "Validation Error");
                return;
            }

            string username = await _consoleHandler.loginUser(email, password);
            weightLeafOfficerTxt_st1.Text = username;
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    statusLabel.Content = "Logging in...";
                    
                });
                

                //window switch here
            });
            statusLabel.Content = "Login Success!";
            // Check which radio button is selected and show the corresponding page
            if (RadioBtnStation1.IsChecked == true)
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

        private bool _isPasswordVisible = false;

        private void TogglePasswordVisibilityClick(object sender, RoutedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                // Hide the visible TextBox and show the PasswordBox.
                // Update the PasswordBox value from the TextBox.
                PasswordBoxControl.Password = VisiblePasswordTextBox.Text;
                VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBoxControl.Visibility = Visibility.Visible;
                _isPasswordVisible = false;
            }
            else
            {
                // Show the TextBox and hide the PasswordBox.
                // Update the TextBox text from the PasswordBox.
                VisiblePasswordTextBox.Text = PasswordBoxControl.Password;
                PasswordBoxControl.Visibility = Visibility.Collapsed;
                VisiblePasswordTextBox.Visibility = Visibility.Visible;
                _isPasswordVisible = true;
            }
        }

        //topbar section
        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsFrame.Visibility = Visibility.Visible;
            Station1Frame.Visibility = Visibility.Collapsed;
            //button visibility logic
            ApplicationSettingsButton.Opacity = 0.6;
            GeneralSettingsButton.Opacity = 1.0;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            GeneralSettingsButtonRightArrow.Visibility = Visibility.Visible;

            GeneralSettingsSection.Visibility = Visibility.Visible;
            ApplicationSettingsSection.Visibility = Visibility.Hidden;
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
            //IntroFrame.Visibility = Visibility.Visible;
            LoginFrame.Visibility = Visibility.Visible;
            StationMainFrame.Visibility = Visibility.Collapsed;
            Station1Frame.Visibility= Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;
        }


        //station1 frame

        private void wieghtScalerConfirmBtn_st1_Click(object sender, RoutedEventArgs e)
        {
            // Check if the status equals "සමබරයි"
            if (weightScalerStatus_st1.Text == "සමබරයි")
            {
                // Example weight text: "36.5KG"
                string weightText = weightScalerValTxt_st1.Text.ToUpper().Replace("KG", "").Trim();
                if (decimal.TryParse(weightText, out decimal weight))
                {
                    // Get the largest integer less than or equal to the specified number
                    int floorValue = (int)Math.Floor((double)weight);
                    // Set the global variable
                    totalLeafWeight_st1 = floorValue;
                    // Update the acceptedLeafWeightTxt_st1 TextBox/TextBlock
                    acceptedLeafWeightTxt_st1.Text = totalLeafWeight_st1.ToString();
                }
                else
                {
                    MessageBox.Show("Invalid weight value.");
                }
            }
        }

        //private bool _isUpdatingWeights = false;

        //private void goldenLeafWeight_st1_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    if (_isUpdatingWeights)
        //        return;

        //    _isUpdatingWeights = true;

        //    // Parse the accepted and golden weights as integers.
        //    if (int.TryParse(acceptedLeafWeightTxt_st1.Text, out int accepted) &&
        //        int.TryParse(goldenLeafWeight_st1.Text, out int golden))
        //    {
        //        // Calculate normal weight: accepted = golden + normal
        //        int normal = accepted - golden;
        //        normalLeafWeight_st1.Text = normal.ToString();
        //    }
        //    else
        //    {
        //        normalLeafWeight_st1.Text = "";
        //    }

        //    _isUpdatingWeights = false;
        //}

        //private void normalLeafWeight_st1_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    if (_isUpdatingWeights)
        //        return;

        //    _isUpdatingWeights = true;

        //    // Parse the accepted and normal weights as integers.
        //    if (int.TryParse(acceptedLeafWeightTxt_st1.Text, out int accepted) &&
        //        int.TryParse(normalLeafWeight_st1.Text, out int normal))
        //    {
        //        // Calculate golden weight: accepted = golden + normal
        //        int golden = accepted - normal;
        //        goldenLeafWeight_st1.Text = golden.ToString();
        //    }
        //    else
        //    {
        //        goldenLeafWeight_st1.Text = "";
        //    }

        //    _isUpdatingWeights = false;
        //}



        private void txtNSacks_TextChanged(object sender, TextChangedEventArgs e)
{
            if (!string.IsNullOrWhiteSpace(txtNSacks.Text))
            {
                // When txtNSacks has text, disable txtNBoxes and update border colors
                txtNBoxes.IsEnabled = false;
                borderNSacks.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // Green highlight
                borderNBoxes.BorderBrush = new SolidColorBrush(Colors.Gray); // Gray out
            }
            else
            {
                // When txtNSacks is empty and txtNBoxes is empty, enable both and revert to default border color
                if (string.IsNullOrWhiteSpace(txtNBoxes.Text))
                {
                    txtNBoxes.IsEnabled = true;
                    borderNSacks.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                    borderNBoxes.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                }
            }
}

private void txtNBoxes_TextChanged(object sender, TextChangedEventArgs e)
{
    if (!string.IsNullOrWhiteSpace(txtNBoxes.Text))
    {
                //nBoxes_st1 = 0;
        // When txtNBoxes has text, disable txtNSacks and update border colors
        txtNSacks.IsEnabled = false;
        borderNBoxes.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // Green highlight
        borderNSacks.BorderBrush = new SolidColorBrush(Colors.Gray); // Gray out
    }
    else
    {
        // When txtNBoxes is empty and txtNSacks is empty, enable both and revert to default border color
        if (string.IsNullOrWhiteSpace(txtNSacks.Text))
        {
                    //nBoxes_st1 = Int32.Parse(txtNBoxes.Text);
                    txtNSacks.IsEnabled = true;
            borderNSacks.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
            borderNBoxes.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
        }
    }
}



        //settings frame
        private void GeneralSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            ApplicationSettingsButton.Opacity = 0.6;
            GeneralSettingsButton.Opacity = 1.0;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            GeneralSettingsButtonRightArrow.Visibility = Visibility.Visible;

            GeneralSettingsSection.Visibility = Visibility.Visible;
            ApplicationSettingsSection.Visibility = Visibility.Hidden;
        }
        private void ApplicationSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            ApplicationSettingsButton.Opacity = 1.0;
            GeneralSettingsButton.Opacity = 0.6;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Visible;
            GeneralSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            GeneralSettingsSection.Visibility = Visibility.Hidden;
            ApplicationSettingsSection.Visibility = Visibility.Visible;
        }



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

        //protected override void OnClosed(EventArgs e)
        //{
        //    runtimeService.OnWindowClosed(); // Clean up resources when the window is closed
        //    base.OnClosed(e);
        //}








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