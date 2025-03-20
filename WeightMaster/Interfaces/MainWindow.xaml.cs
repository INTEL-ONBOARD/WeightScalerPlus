using Microsoft.Win32;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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

        private int _enterPressCount = 0; // Tracks the number of Enter presses

        public int nSacks_st1 = 0;
        public int nBoxes_st1 = 0;
        public int singleBoxWeight = 10;


        // Global integer for the total leaf weight(floor value)
        public int totalLeafWeight_st1 = 0;
        public int totalBoxWeight_st1 = 0;

        public int currentAcceptedLeafWeight_st1 = 0;
        private int currentGoldenLeafWeight_st1 = 0;
        private int currentNormalLeafWeight_st1 = 0;

        public int currentTotalDeduction_st1 = 0;

        //barcode related__________________________________________________________________
        private StringBuilder _barcodeBuffer = new StringBuilder();
        private DispatcherTimer _timer;
        private DateTime _lastKeyTime;

        [DllImport("user32.dll")]
        static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder pwszBuff,
            int cchBuff, uint wFlags, IntPtr dwhkl);

        [DllImport("user32.dll")]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);
        //__________________________________________________________________________________|


        //private string filePath;
        //private FileSystemWatcher fileWatcher;
        //private DispatcherTimer readTimer;

        //private void BrowseButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // Open a file dialog to select the JSON file.
        //    var dialog = new Microsoft.Win32.OpenFileDialog
        //    {
        //        Filter = "JSON files (.json)|.json|All files (.)|."
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
            runtimeService = new Runtime(this, path); // Pass the labels from XAML
            OpenCustomerWindow();
            _consoleHandler = new ConsoleHandler();
            // Handles key presses globally(currently used to handle Enter key press)
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            //barcode related 
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += Timer_Tick;
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true; // Prevent other controls from handling Enter
                ExecuteEnterButtonSteps();
                //_enterPressCount = (_enterPressCount + 1) % 3; // Cycle 0→1→2→0... //this was done in the click event manually to go with manual button click flows
            }
        }

        private void ExecuteEnterButtonSteps()
        {
            switch (_enterPressCount)
            {
                case 0:
                    wieghtScalerConfirmBtn_st1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step1 click
                    break;
                case 1:
                    confirmAddRowButton_st1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step2 click
                    MessageBox.Show("step 2 triggered");
                    break;
                case 2:
                    confirmAll_rounds_st1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step3 click
                    MessageBox.Show("step 3 triggered");
                    break;
            }
        }

        //barcode related logic_______________________________________________________________________________________________
        private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == 0x100) // WM_KEYDOWN
            {
                var key = KeyInterop.KeyFromVirtualKey((int)msg.wParam);
                if (key == Key.Enter)
                {
                    // Handle Enter key and suppress if it's part of a barcode
                    handled = HandleEnterKey();
                }
                else
                {
                    char c = GetCharFromKey(key);
                    if (c != '\0')
                    {
                        _lastKeyTime = DateTime.Now;
                        _barcodeBuffer.Append(c);
                        _timer.Stop();
                        _timer.Start();

                        // Suppress this keystroke from reaching other controls
                        //handled = true; // <-- Prevents input to other TextBoxes
                    }
                }
            }
        }

        private bool HandleEnterKey()
        {
            _timer.Stop();
            bool isBarcode = false;

            if ((DateTime.Now - _lastKeyTime).TotalMilliseconds < 100 && _barcodeBuffer.Length > 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Station1Frame.IsVisible)
                    {
                        barcodeTxt_st1.Text = _barcodeBuffer.ToString();
                    }
                    if (Station2Frame.IsVisible)
                    {
                        barcodeTxt_st2.Text = _barcodeBuffer.ToString();
                    }
                    _barcodeBuffer.Clear();
                }));
                isBarcode = true; // Barcode detected
            }
            else
            {
                _barcodeBuffer.Clear();
            }

            return isBarcode; // Enter is handled only if part of a barcode
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            _barcodeBuffer.Clear();
        }

        private char GetCharFromKey(Key key)
        {
            // Use a StringBuilder instead of char[]
            StringBuilder buffer = new StringBuilder(64); // 64 matches SizeConst in the P/Invoke
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);
            IntPtr inputLocale = GetKeyboardLayout(0);

            // Call ToUnicodeEx with the StringBuilder
            int result = ToUnicodeEx(
                (uint)virtualKey,
                0,
                keyboardState,
                buffer,
                buffer.Capacity, // Pass buffer capacity as the buffer size
                0,
                inputLocale
            );

            return result == 1 ? buffer[0] : '\0';
        }
        //________________________________end of barcode logic_____________________________________________________________________________________



        public async Task testExecution()
        {

            await _consoleHandler.GetLineMasterAsync();
        }

        //made this global to used on the close event
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
            await _consoleHandler.GetLineMasterAsync();
            //checks db records whether they exists
            await _consoleHandler.VerifyUserDb();
            System.Diagnostics.Debug.WriteLine("> calling start");
            await _consoleHandler.VerifyLineMasterDb();
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


            if (username.Equals(""))
            {
                statusLabel.Content = "Login Success!";
                // Check which radio button is selected and show the corresponding page
                if (RadioBtnStation1.IsChecked == true)
                {
                    LoginFrame.Visibility = Visibility.Collapsed;
                    StationMainFrame.Visibility = Visibility.Visible;
                    Station1Frame.Visibility = Visibility.Visible;
                    Station2Frame.Visibility = Visibility.Collapsed;
                    //change topbar text
                    TopBarText.Text = "වේදිකාව-1";

                }
                else if (RadioBtnStation2.IsChecked == true)
                {
                    LoginFrame.Visibility = Visibility.Collapsed;
                    StationMainFrame.Visibility = Visibility.Visible;
                    Station2Frame.Visibility = Visibility.Visible;
                    Station1Frame.Visibility = Visibility.Collapsed;

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
            else {
                MessageBox.Show("කරුණාකර නිවැරදි තොරතුරු අතුලත් කරන්න");
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
            Station1Frame.Visibility = Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;
        }


        //station1 frame_______________________________________________________________________________________________________________________

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
                    // Update the acceptedLeafWeightTxt_st1 TextBox/TextBlock
                    acceptedLeafWeightTxt_st1.Text = floorValue.ToString();
                    currentAcceptedLeafWeight_st1 = floorValue;

                    //to catch up with Enter key press event(in case of the manual click)
                    _enterPressCount = 1; // Cycle 0→1→2→0...
                }
                else
                {
                    MessageBox.Show("Invalid weight value.");
                }
            }
        }


        private void nSacksTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(nSacksTxt_st1.Text))
            {
                // When txtNSacks has text, disable txtNBoxes and update border colors
                nBoxesTxt_st1.IsEnabled = false;
                nBoxesLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEFEF")); // Gray out EFEFEF
                borderNBoxes_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEFEF")); // Gray out EFEFEF
                borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // Green highlight
                nSacksLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
            }
            else
            {
                // When txtNSacks is empty and txtNBoxes is empty, enable both and revert to default border color
                if (string.IsNullOrWhiteSpace(nBoxesTxt_st1.Text))
                {
                    nBoxesTxt_st1.IsEnabled = true;
                    borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                    borderNBoxes_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                    nSacksLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                    nBoxesLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                }
            }
        }

        private void nBoxesTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(nBoxesTxt_st1.Text))
            {
                //nBoxes_st1 = 0;
                // When txtNBoxes has text, disable txtNSacks and update border colors
                nSacksTxt_st1.IsEnabled = false;
                nSacksLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEFEF")); // Gray out EFEFEF
                borderNBoxes_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // Green highlight
                borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFEFEF")); // Gray out
                nBoxesLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
            }
            else
            {
                // When txtNBoxes is empty and txtNSacks is empty, enable both and revert to default border color
                if (string.IsNullOrWhiteSpace(nSacksTxt_st1.Text))
                {
                    //nBoxes_st1 = Int32.Parse(txtNBoxes.Text);
                    nSacksTxt_st1.IsEnabled = true;
                    borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                    borderNBoxes_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                    nSacksLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                    nBoxesLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                }
            }
        }

        //gold and normal leaf textbox logic
        private void GoldenAndNormalWeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check if controls exist (avoids NullReferenceException during initialization)
            if (acceptedLeafWeightTxt_st1 == null)
                return;
            // Parse values (handle empty/invalid input)
            if (!int.TryParse(acceptedLeafWeightTxt_st1.Text, out int acceptedLeafWeight) || acceptedLeafWeight < 0)
                acceptedLeafWeight = 0;
            if (!int.TryParse(goldenLeafWeightTxt_st1.Text, out int goldenLeafWeight) || goldenLeafWeight < 0)
                goldenLeafWeight = 0;
            if (!int.TryParse(normalLeafWeightTxt_st1.Text, out int normalLeafWeight) || normalLeafWeight < 0)
                normalLeafWeight = 0;

            //calculate total leaf
            int total = normalLeafWeight + goldenLeafWeight;

            //available weights for golden and normal leaf weights after the deductions
            int availableWeight = total - currentTotalDeduction_st1;

            if (goldenLeafWeightTxt_st1.Text.Equals("0") && normalLeafWeightTxt_st1.Text.Equals("0")) { return; }

            //if golden leaf weight was changed
            if (goldenLeafWeightTxt_st1.IsKeyboardFocused)
            {
                //MessageBox.Show("goldleaf: event triggered.");
                normalLeafWeightTxt_st1.Text = ((acceptedLeafWeight - currentTotalDeduction_st1) - goldenLeafWeight).ToString();
                //update helper variables
                currentGoldenLeafWeight_st1 = goldenLeafWeight;
                currentNormalLeafWeight_st1 = (currentAcceptedLeafWeight_st1 + currentTotalDeduction_st1) - goldenLeafWeight;
            }

            //for testing purposes
            greenText.Text = currentNormalLeafWeight_st1.ToString();
            goldText.Text = currentGoldenLeafWeight_st1.ToString();
        }

        //to block invalid user inputs
        private void GoldenAndNormalWeight_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block non-digit characters
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            // Get the proposed new text (current text + new input)
            var textBox = (TextBox)sender;
            string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength) + e.Text;

            // Check if the proposed text is a valid integer and less than 30
            if (int.TryParse(proposedText, out int inputNumber) && inputNumber > currentAcceptedLeafWeight_st1)
            {
                e.Handled = true; // Block the input
                MessageBox.Show(proposedText + " exceeds Accepted leaf weight.");
            }

        }



        //weight deduction logic
        private void WeightDeducation_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check if controls exist (avoids NullReferenceException during initialization)
            if (wateredTxt_st1 == null || rejectedTxt_st1 == null || maturedTxt_st1 == null || spoiledTxt_st1 == null || currentNormalLeafWeight_st1 == null)
                return;
            // Parse values (handle empty/invalid input)
            if (!int.TryParse(wateredTxt_st1.Text, out int watered) || watered < 0)
                watered = 0;
            if (!int.TryParse(rejectedTxt_st1.Text, out int rejected) || rejected < 0)
                rejected = 0;
            if (!int.TryParse(maturedTxt_st1.Text, out int matured) || matured < 0)
                matured = 0;
            if (!int.TryParse(spoiledTxt_st1.Text, out int spoiled) || spoiled < 0)
                spoiled = 0;

            // Calculate total
            int totalDeductions = watered + rejected + matured + spoiled;

            //choose deduction type between green leaves or golden leaves based on total
            //if normal weight doesn't exceeds total deduction(no need to update golden leaf weights)
            if (totalDeductions <= currentNormalLeafWeight_st1)
            {
                // Update normalLeafWeight
                normalLeafWeightTxt_st1.Text = (currentNormalLeafWeight_st1 - totalDeductions).ToString();
                goldenLeafWeightTxt_st1.Text = currentGoldenLeafWeight_st1.ToString();
                //update helper value
                currentTotalDeduction_st1 = totalDeductions;
                blueText.Text =  currentTotalDeduction_st1.ToString();
            }
            //if normal weight doesn't exceeds total deduction(now you need to update both golden leaf weights & normal leaf weights)
            else if (totalDeductions > currentNormalLeafWeight_st1)
            {
                MessageBox.Show("here triggered");
                normalLeafWeightTxt_st1.Text = "0";
                goldenLeafWeightTxt_st1.Text = (currentGoldenLeafWeight_st1 - (totalDeductions - currentNormalLeafWeight_st1)).ToString();
                //update helper value
                currentTotalDeduction_st1 = totalDeductions;
                blueText.Text = currentTotalDeduction_st1.ToString();
            }
        }

        //to block invalid user inputs for weight deduction
        private void WeightDeduction_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block non-digit characters
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }
            // Get the proposed new text (current text + new input)
            var textBox = (TextBox)sender;
            string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength) + e.Text;

            // Check if the proposed text is a valid integer and doesn't exeeds total weight
            if (int.TryParse(proposedText, out int inputNumber) && inputNumber > currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1)
            {
                e.Handled = true; // Block the input
                MessageBox.Show(proposedText + " Deduction Error: exceeds golden and normal leaf(" + (currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) + ") weight.");
            }
        }

        //clear button
        private void clearBtn_st1_Clicked(object sender, RoutedEventArgs e)
        {
            wateredTxt_st1.Text = "0";
            rejectedTxt_st1.Text = "0";
            spoiledTxt_st1.Text = "0";
            maturedTxt_st1.Text = "0";
            normalLeafWeightTxt_st1.Text = "0";
            goldenLeafWeightTxt_st1.Text = "0";
            //keep these empty else both textboxes gets disabled by logic.
            nSacksTxt_st1.Text = "";
            nBoxesTxt_st1.Text = "";
        }

        private int _currentTurn = 1;
        private void confirmAddRowButton_st1_Click(object sender, RoutedEventArgs e)
        {
            //load table rows(test)
            if (!acceptedLeafWeightTxt_st1.Text.Equals("") && !goldenLeafWeightTxt_st1.Text.Equals("") && !normalLeafWeightTxt_st1.Text.Equals("") && ((!nSacksTxt_st1.Text.Equals("") && nBoxesTxt_st1.Text.Equals("")) || (nSacksTxt_st1.Text.Equals("") && !nBoxesTxt_st1.Text.Equals(""))))
            {
                _enterPressCount=2;
                int totalWeight = (Convert.ToInt32(goldenLeafWeightTxt_st1.Text) + Convert.ToInt32(normalLeafWeightTxt_st1.Text));
                Station1TableRow station1TableRow1 = new Station1TableRow(_currentTurn++.ToString(), nSacksTxt_st1.Text, nBoxesTxt_st1.Text, totalWeight.ToString(), goldenLeafWeightTxt_st1.Text, normalLeafWeightTxt_st1.Text);
                Station1TablePanel.Children.Add(station1TableRow1);
            }
            else 
            {
                MessageBox.Show("කරුණාකර සියලු තොරතුරු අතුලත් කරන්න");
            }
        }


        //settings frame______________________________________________________________________________________________________________________________
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
            openFileDialog.Filter = "Text files (.txt)|.txt|All files (.)|.";
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

        private void finishButton_st1_Click(object sender, RoutedEventArgs e)
        {
            _enterPressCount = 0; //0 1 2

            wateredTxt_st1.Text = "";
            rejectedTxt_st1.Text = "";
            spoiledTxt_st1.Text = "";
            maturedTxt_st1.Text = "";
            normalLeafWeightTxt_st1.Text = "";
            goldenLeafWeightTxt_st1.Text = "";
            //keep these empty else both textboxes gets disabled by logic.
            nSacksTxt_st1.Text = "";
            nBoxesTxt_st1.Text = "";

            goldenLeafWeightTxt_st1.Text = "";
            normalLeafWeightTxt_st1.Text = "";
            acceptedLeafWeightTxt_st1.Text = "";

        }
    }
}