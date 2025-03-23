using Microsoft.Win32;
using System.Configuration;
using System.IO;
using System.Reflection.Emit;
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
using WeightMaster.Models;
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
        private double singleBoxWeight = 3;


        // Global integer for the total leaf weight(floor value)
        public int totalLeafWeight_st1 = 0;
        public int totalBoxWeight_st1 = 0;

        public int currentAcceptedLeafWeight_st1 = 0;
        private double currentGoldenLeafWeight_st1 = 0;
        private double currentNormalLeafWeight_st1 = 0;

        public double currentTotalDeduction_st1 = 0;
        //public double currentTotalBoxWeight = 0;

        //used to count the number of rounds(for the table indexing and other purposes if necessary)
        private int _currentTurn = 1;
        //finalized values by each round to send to db/api
        private float finalWeightScalerWeight_st1 = 0; //this goes as the accepted value to api
        private int finalAcceptedLeafWeight_st1 = 0;
        private int finalGoldenLeafWeight_st1 = 0;
        private int finalNormalLeafWeight_st1 = 0;

        private int finalNBoxes_st1 = 0;
        private int finalNSacks_st1 = 0;

        private int finalWateredWeight_st1 = 0;
        private int finalMaturedWeight_st1 = 0;
        private int finalSpoiledWeight_st1 = 0;
        private int finalRejectedWeight_st1 = 0;

        private int finalAvailableGoldenLeafWeight_st1 = 0;
        private int finalAvailableNormalLeafWeight_st1 = 0;
        //private int finalAvailableLeafWeight = 0;

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


        public MainWindow()
        {
            InitializeComponent();
            //Topbar
            runtimeService = new Runtime(this, path); // Pass the labels from XAML
            //OpenCustomerWindow(); //this was moved to the login to trigger this upon login.
            //this console handler is used globally
            _consoleHandler = new ConsoleHandler();
            // Handles key presses globally(currently used to handle Enter key press)
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            //barcode related 
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += Timer_Tick;
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;

            StartupTheAppAsync();
        }

        private async void StartupTheAppAsync()
        {
            //closing existing pages
            Station1Frame.Visibility = Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;
            SettingsFrame.Visibility = Visibility.Collapsed;
            StationMainFrame.Visibility = Visibility.Collapsed;
            IntroFrame.Visibility = Visibility.Visible;

            //repeat until no exception is occured.
            bool failed = false;
            do {
                try
                {
                    //Verifying User Database
                    // Update status label: "Verifying User Database..."
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "Verifying Database Status(1)...";
                    });
                    // Await the asynchronous operation
                    await _consoleHandler.VerifyUserDb();
                    // Once verification is complete, update the status label again
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(1)";
                    });
                    // Optionally, wait a short moment to show the completion status, then hide the IntroFrame
                    //await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"User Database Error: {ex.Message}";
                    });
                }

                //Verifying LineMaster Database
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "Verifying Database Status(2)...";
                    });
                    await _consoleHandler.VerifyLineMasterDb();
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(2)";
                    });
                    //await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"LineMaster Database Error: {ex.Message}";
                    });
                }

                //Verifying Member Database
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "Verifying Database Status(3)...";
                    });
                    await _consoleHandler.verifyMemberDb();
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(3)";
                    });
                    //await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"Member Database Error: {ex.Message}";
                    });
                }
            }
            while (failed);

            IntroFrame.Visibility = Visibility.Collapsed;
            LoginFrame.Visibility = Visibility.Visible;
            
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
            if (Station1Frame.IsVisible && !Station2Frame.IsVisible)
            {
                switch (_enterPressCount)
                {
                    case 0:
                        wieghtScalerConfirmBtn_st1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step1 click
                        break;
                    case 1:
                        confirmAddRowButton_st1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step2 click
                                                                                                    //MessageBox.Show("step 2 triggered");
                        break;
                    case 2:
                        confirmAll_rounds_st1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step3 click
                                                                                                  //MessageBox.Show("step 3 triggered");
                        break;
                }
            }
            if (Station1Frame.IsVisible && !Station2Frame.IsVisible)
            {
                switch (_enterPressCount)
                {
                    case 0:
                        wieghtScalerConfirmBtn_st2.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step1 click
                        break;
                    case 1:
                        confirmAddRowButton_st2.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step2 click
                                                                                                    //MessageBox.Show("step 2 triggered");
                        break;
                    case 2:
                        confirmAll_rounds_st2.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Trigger step3 click
                                                                                                  //MessageBox.Show("step 3 triggered");
                        break;
                }
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



        //public async Task testExecution()
        //{

        //    await _consoleHandler.GetLineMasterAsync();
        //}

        //made this global to used on the close event
        private CustomerWindow customerWindow;
        private void OpenCustomerWindow()
        {
            customerWindow = new CustomerWindow(this);

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

        List<LineMasterBlockModel> lineMasterData = null;
        List<String> supervisorData = null;

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {

            System.Diagnostics.Debug.WriteLine("> calling start");
            lineMasterData = await _consoleHandler.getLineMasterData();
            if (lineMasterData != null && lineMasterData.Any())
            {
                foreach (var lineMaster in lineMasterData)
                {
                    System.Diagnostics.Debug.WriteLine($"ID: {lineMaster.id}, Line Name: {lineMaster.LineName}, Line Master: {lineMaster.LineMaster}");
                    lineNameCmb_st1.Items.Add(lineMaster.LineName);

                }
            }
            System.Diagnostics.Debug.WriteLine("> calling done");

            System.Diagnostics.Debug.WriteLine("> fetching supervisor data");
            supervisorData = await _consoleHandler.getUsernames();
            if (supervisorData != null && supervisorData.Any())
            {
                foreach (string supervisor in supervisorData)
                {
                    System.Diagnostics.Debug.WriteLine($"supervisor Name: {supervisor}");
                    //adding supervisors to both stations
                    supervisorCmb_st1.Items.Add(supervisor);
                    supervisorCmb_st2.Items.Add(supervisor);

                }
            }
            System.Diagnostics.Debug.WriteLine("> supervisor fetching done");

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
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    statusLabel.Content = "Logging in...";

                });


                //window switch here
            });


            if (/*!username.Equals("unknown")*/true)
            {
                statusLabel.Content = "Login Success!";
                statusLabel.Content = "";

                //opening customer window
                OpenCustomerWindow();

                // Check which radio button is selected and show the corresponding page
                if (RadioBtnStation1.IsChecked == true)
                {
                    //load the username as the leaf weight officer
                    weightLeafOfficerTxt_st1.Text = username;

                    LoginFrame.Visibility = Visibility.Collapsed;
                    StationMainFrame.Visibility = Visibility.Visible;
                    Station1Frame.Visibility = Visibility.Visible;
                    Station2Frame.Visibility = Visibility.Collapsed;
                    //change topbar text
                    //TopBarText.Text = "වේදිකාව-1";

                }
                else if (RadioBtnStation2.IsChecked == true)
                {

                    //load the username as the leaf weight officer
                    weightLeafOfficerTxt_st2.Text = username;

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
                    //TopBarText.Text = "වේදිකාව-2";
                }
                else if (RadioBtnAdmin.IsChecked == true)
                {
                    //PageAdmin.Visibility = Visibility.Visible;
                }
            }
            else {
                statusLabel.Content = "Login Failed...";
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

            //clear existing data
            //clear weight leaf cmb officer data
            lineNameCmb_st1.Items.Clear();
            lineNameCmb_st2.Items.Clear();
            //clear line master cmb data
            supervisorCmb_st1.Items.Clear();
            supervisorCmb_st2.Items.Clear();

            //hmm you need either to clear all textboxes or restart the app.

        }


        //station1 frame_______________________________________________________________________________________________________________________

        private async void barcodeTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            string request = barcodeTxt_st1.Text;
            bool isSuccess = true;
            string response = "";
            if (isSuccess)
            {

                System.Diagnostics.Debug.WriteLine("> calling start");
                String memberName = await _consoleHandler.GetMemberName(request);

                System.Diagnostics.Debug.WriteLine(request+": "+ memberName);
                customerNameTxt_st1.Text = memberName;

            }
            else {
                //show red line
                customerNameTxt_st1.Text = "-";
                System.Diagnostics.Debug.WriteLine("Barcode data failed/not found");
            }
        }

        private void lineNameCmb_st1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string searchLineName = lineNameCmb_st1.SelectedItem.ToString(); // replace with the line name you're searching for
            var result = lineMasterData.FirstOrDefault(item => item.LineName == searchLineName);
            if (result != null)
            {
                lineMasterNameLbl_st1.Text = result.LineMaster;
            }
            else
            {
                lineMasterNameLbl_st1.Text = "-";
            }
        }


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


        

        //gold and normal leaf textbox logic
        private void GoldenAndNormalWeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check if controls exist (avoids NullReferenceException during initialization)
            if (acceptedLeafWeightTxt_st1 == null)
                return;
            // Parse values (handle empty/invalid input)
            if (!double.TryParse(acceptedLeafWeightTxt_st1.Text, out double acceptedLeafWeight) || acceptedLeafWeight < 0)
                acceptedLeafWeight = 0;
            if (!double.TryParse(goldenLeafWeightTxt_st1.Text, out double goldenLeafWeight) || goldenLeafWeight < 0)
                goldenLeafWeight = 0;
            if (!double.TryParse(normalLeafWeightTxt_st1.Text, out double normalLeafWeight) || normalLeafWeight < 0)
                normalLeafWeight = 0;

            //calculate total leaf
            double total = normalLeafWeight + goldenLeafWeight;

            //available weights for golden and normal leaf weights after the deductions
            //double availableWeight = total - currentTotalDeduction_st1;

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
            if (wateredTxt_st1 == null || rejectedTxt_st1 == null || maturedTxt_st1 == null || spoiledTxt_st1 == null || currentNormalLeafWeight_st1 == null || nSacksTxt_st1 == null)
                return;
            // Parse values (handle empty/invalid input)
            if (!double.TryParse(wateredTxt_st1.Text, out double watered) || watered < 0)
                watered = 0;
            if (!double.TryParse(rejectedTxt_st1.Text, out double rejected) || rejected < 0)
                rejected = 0;
            if (!double.TryParse(maturedTxt_st1.Text, out double matured) || matured < 0)
                matured = 0;
            if (!double.TryParse(spoiledTxt_st1.Text, out double spoiled) || spoiled < 0)
                spoiled = 0;
            if (!double.TryParse(nBoxesTxt_st1.Text, out double nBoxes) || spoiled < 0)
                nBoxes = 0;
            double boxWeights = nBoxes * singleBoxWeight;
            // Calculate total
            double totalDeductions = watered + rejected + matured + spoiled + boxWeights;

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
                //MessageBox.Show("here triggered");
                normalLeafWeightTxt_st1.Text = "0";
                goldenLeafWeightTxt_st1.Text = (currentGoldenLeafWeight_st1 - (totalDeductions - currentNormalLeafWeight_st1)).ToString();
                //update helper value
                currentTotalDeduction_st1 = totalDeductions;
                blueText.Text = currentTotalDeduction_st1.ToString();
            }

            //this logic is used with number of sacks textbox being disable and stuff
            if (nBoxesTxt_st1.IsFocused) {
                if (!string.IsNullOrWhiteSpace(nBoxesTxt_st1.Text))
                {
                    //nBoxes_st1 = 0;
                    // When txtNBoxes has text, disable txtNSacks and update border colors
                    //nSacksTxt_st1.IsEnabled = false;  //i already handled in preview input event.
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
                        //nSacksTxt_st1.IsEnabled = true; //i already handled in preview input event.
                        borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                        borderNBoxes_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                        nSacksLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                        nBoxesLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                    }
                }
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









        private void BoxWeightDeduction_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block non-digit characters
            if (!char.IsDigit(e.Text, 0) || !nSacksTxt_st1.Text.Equals(""))
            {
                e.Handled = true;
                return;
            }
            //Get the proposed new text(current text + new input)
            var textBox = (TextBox)sender;
            string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength) + e.Text;

            //Check if the proposed text is a valid integer and THE TOTAL BOX WIEGHT(num of boxes* single box weight) doesn't exceeds total weight
            if (int.TryParse(proposedText, out int inputNumber) && (inputNumber * singleBoxWeight) > currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1)
            {
                e.Handled = true; // Block the input
                MessageBox.Show((inputNumber * singleBoxWeight).ToString() + " Deduction Error: exceeds golden and normal leaf(" + (currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) + ") weight.");
            }
        }

        private void nSacksTxt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block non-digit characters
            if (!char.IsDigit(e.Text, 0) || !nBoxesTxt_st1.Text.Equals(""))
            {
                e.Handled = true;
                return;
            }
        }

        private void nSacksTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(nSacksTxt_st1.Text))
            {
                // When txtNSacks has text, disable txtNBoxes and update border colors
                //nBoxesTxt_st1.IsEnabled = false; //i already handled in preview input event.
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
                    //nBoxesTxt_st1.IsEnabled = true; //i already handled in preview input event.
                    borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                    borderNBoxes_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                    nSacksLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                    nBoxesLbl_st1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")); // textbox label color
                }
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

        
        private void confirmAddRowButton_st1_Click(object sender, RoutedEventArgs e)
        {
            //load table rows(test)
            if (!acceptedLeafWeightTxt_st1.Text.Equals("") && !goldenLeafWeightTxt_st1.Text.Equals("") && !normalLeafWeightTxt_st1.Text.Equals("") && ((!nSacksTxt_st1.Text.Equals("") && nBoxesTxt_st1.Text.Equals("")) || (nSacksTxt_st1.Text.Equals("") && !nBoxesTxt_st1.Text.Equals(""))))
            {
                _enterPressCount =2;
                int totalWeight = (Convert.ToInt32(goldenLeafWeightTxt_st1.Text) + Convert.ToInt32(normalLeafWeightTxt_st1.Text));
                Station1TableRow station1TableRow1 = new Station1TableRow(_currentTurn++.ToString(), nSacksTxt_st1.Text, nBoxesTxt_st1.Text, totalWeight.ToString(), goldenLeafWeightTxt_st1.Text, normalLeafWeightTxt_st1.Text);
                Station1TablePanel.Children.Add(station1TableRow1);

                float.TryParse(weightScalerValTxt_st1.Text, out float finalWeightScalerValue);
                int.TryParse(acceptedLeafWeightTxt_st1.Text, out int finalAcceptedLeafWeight);

                int.TryParse(nBoxesTxt_st1.Text, out int finalNBoxes);
                int.TryParse(nSacksTxt_st1.Text, out int finalNSacks);

                int.TryParse(wateredTxt_st1.Text, out int finalWateredWeight);
                int.TryParse(maturedTxt_st1.Text, out int finalMaturedWeight);
                int.TryParse(spoiledTxt_st1.Text, out int finalSpoiledWeight);
                int.TryParse(rejectedTxt_st1.Text, out int finalRejectedWeight);

                int.TryParse(normalLeafWeightTxt_st1.Text, out int finalAvailableNormalLeafWeight);
                int.TryParse(goldenLeafWeightTxt_st1.Text, out int finalAvailableGoldenLeafWeight);

                //preparing values for the finish api command
                finalWeightScalerWeight_st1 += finalWeightScalerValue;
                finalAcceptedLeafWeight_st1 += finalAcceptedLeafWeight;

                finalNormalLeafWeight_st1 += (int)currentNormalLeafWeight_st1;
                finalGoldenLeafWeight_st1 += (int)currentGoldenLeafWeight_st1;

                finalNBoxes_st1 += finalNBoxes;
                finalNSacks_st1 += finalNSacks;

                finalWateredWeight_st1 += finalWateredWeight;
                finalMaturedWeight_st1 += finalMaturedWeight;
                finalSpoiledWeight_st1 += finalSpoiledWeight;
                finalRejectedWeight_st1 += finalRejectedWeight;

                finalAvailableGoldenLeafWeight_st1 += finalAvailableGoldenLeafWeight;
                finalAvailableNormalLeafWeight_st1 += finalAvailableNormalLeafWeight;
                //System.Diagnostics.Debug.WriteLine("Added value: "+ finalWeightScalerWeight_st1);

                //if everything was added successfully, clear all textboxes(except table row, member & line master/name details)
    }
            else 
            {
                MessageBox.Show("කරුණාකර සියලු තොරතුරු අතුලත් කරන්න");
            }
        }

        //station2 frame______________________________________________________________________________________________________________________________

        private async void barcodeTxt_st2_TextChanged(object sender, TextChangedEventArgs e)
        {
            string request = barcodeTxt_st2.Text;
            bool isSuccess = true;
            string response = "";
            if (isSuccess)
            {

                System.Diagnostics.Debug.WriteLine("> calling start");
                String memberName = await _consoleHandler.GetMemberName(request);

                System.Diagnostics.Debug.WriteLine(request + ": " + memberName);
                customerNameTxt_st2.Text = memberName;

            }
            else
            {
                //show red line
                customerNameTxt_st2.Text = "-";
                System.Diagnostics.Debug.WriteLine("Barcode data failed/not found");
            }
        }

        private void lineNameCmb_st2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string searchLineName = lineNameCmb_st2.SelectedItem.ToString(); // replace with the line name you're searching for
            var result = lineMasterData.FirstOrDefault(item => item.LineName == searchLineName);
            if (result != null)
            {
                lineMasterNameLbl_st2.Text = result.LineMaster;
            }
            else
            {
                lineMasterNameLbl_st2.Text = "-";
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

        private async void finishButton_st1_Click(object sender, RoutedEventArgs e)
        {


            var newTransaction = new TransactionLogBlockModel
            {
                linename = lineNameCmb_st1.SelectedValue.ToString(),
                transportagent = lineMasterNameLbl_st1.Text,
                company = "නව ඇලන්වැලි තේ කම්හල",
                leaf_weight_officer = weightLeafOfficerTxt_st1.Text,
                superviosr = supervisorCmb_st1.SelectedValue.ToString(),
                barcode_details = barcodeTxt_st1.Text,
                name_with_initials = customerNameTxt_st1.Text,
                phone_number = "123-456-7890",
                date = DateTime.Now.ToString("yyyy-MM-dd"), // Assuming you want the date in "YYYY-MM-DD" format

                box_count = finalNBoxes_st1,
                bag_count = finalNSacks_st1,
                real_value = finalWeightScalerWeight_st1,

                maximum_nomal_leaf_weight = 90,
                total_leaf_weight = finalAcceptedLeafWeight_st1,
                actual_nomal_leaf_weight = finalNormalLeafWeight_st1,
                total_gold_leaf_weight = finalGoldenLeafWeight_st1,

                water = finalWateredWeight_st1,
                morapuwata = finalMaturedWeight_st1,
                thambimata = finalSpoiledWeight_st1,
                reject = finalRejectedWeight_st1,
                box_weight = finalNBoxes_st1 * (int)singleBoxWeight,

                final_green_leaf_count = finalAvailableNormalLeafWeight_st1,
                final_gold_leaf_count = finalAvailableGoldenLeafWeight_st1
            };

            System.Diagnostics.Debug.WriteLine(newTransaction);
            // Call the service to add the transaction
            bool _isok = await _consoleHandler.AddTransactionAsync(newTransaction);

            if (_isok)
            {
                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ ongoing cloud update ]::::::::::::");

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

            bool _isoks= await _consoleHandler.verifyTransactionsCloudCheck();
            if (_isoks)
            {
                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked done! ]::::::::::::");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked failed! ]::::::::::::");
            }

            var transactions = await _consoleHandler.GetTransactionData();

            // Loop through each transaction and print details to the debug console
            for (int i = 0; i < transactions.Count; i++)
            {
                var transaction = transactions[i];
                System.Diagnostics.Debug.WriteLine($"Transaction {i + 1}:");
                System.Diagnostics.Debug.WriteLine($"Linename: {transaction.linename}");
                System.Diagnostics.Debug.WriteLine($"Transport Agent: {transaction.transportagent}");
                System.Diagnostics.Debug.WriteLine($"Company: {transaction.company}");
                System.Diagnostics.Debug.WriteLine($"Bag Count: {transaction.bag_count}");
                System.Diagnostics.Debug.WriteLine($"Total Leaf Weight: {transaction.total_leaf_weight}");
                System.Diagnostics.Debug.WriteLine("------------------------------");
            }

            System.Diagnostics.Debug.WriteLine("All transaction data printed successfully!");





            var Finaltransaction = new FinalTransactionBlockModel
            {
                linename = "ලංකාගම",
                transportagent = "ජේ.පී දිල්මා දිල්හානි",
                company = "නව ඇලන්වැලි තේ කම්හල",
                leaf_weight_officer = "greenleaf null",
                superviosr = "Administrator",
                barcode_details = "002",
                name_with_initials = "කේ.එ.ගුණපාල",
                phone_number = "0712345678",
                date = "2025-02-06",
                bag_count = 3,
                maximum_nomal_leaf_weight = 69,
                total_leaf_weight = 60,
                actual_nomal_leaf_weight = 30,
                total_gold_leaf_weight = 30,
                water = 1,
                morapuwata = 1,
                thambimata = 1,
                reject = 1,
                bag_weight = 3,
                final_green_leaf_count = 23,
                final_gold_leaf_count = 30,
                real_value = 20
            };

            bool _isdone = await _consoleHandler.AddFinalTransactionAsync(Finaltransaction);

            if (_isdone)
            {
                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ ***************]::::::::::::");

            }

        }
    }
}




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



