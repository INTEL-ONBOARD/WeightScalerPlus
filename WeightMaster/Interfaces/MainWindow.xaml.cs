using Microsoft.Win32;
using System.ComponentModel;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WeightMaster.Core;
using WeightMaster.Interfaces;
using WeightMaster.Interfaces.UserControls;
using WeightMaster.Models;
using WeightMaster.Services;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;

namespace WeightMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Enter & Esc button logic steps
        private int _lastExecutedStep = -1;
        private int _previousStep = -1;
        private bool _isEnterKey = false;
        private string _activeStation = null;
        private bool IsStation1Active() => Station1Frame.IsVisible;
        private bool IsStation2Active() => Station2Frame.IsVisible;

        private int TotalSteps
        {
            get
            {
                if (IsStation1Active()) return 10;
                if (IsStation2Active()) return 8; // Reduced from 9 to 8 steps
                return 0;
            }
        }

        private Runtime runtimeService;
        private String path;
        private ConsoleHandler _consoleHandler;


        private bool userTyped = true;
        //public int nSacks_st1 = 0;
        //public int nBoxes_st1 = 0;
        private double singleBoxWeight = 3.5;


        // Global integer for the total leaf weight(floor value)
        //public int totalLeafWeight_st1 = 0;
        //public int totalBoxWeight_st1 = 0;

        //___________________st1____________________________________________________________________________________________|

        private string lineName_st1 = "";
        private string supervisor_st1 = "";
        private double scalerRoundedWeight_st1 = 0;
        public int currentAcceptedLeafWeight_st1 = 0;
        private double currentGoldenLeafWeight_st1 = 0;
        private double currentNormalLeafWeight_st1 = 0;

        public double currentTotalDeduction_st1 = 0;
        //public double currentTotalBoxWeight = 0;

        //used to count the number of rounds(for the table indexing and other purposes if necessary)
        private int _currentTurn_st1 = 1;
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

        //___________________st2____________________________________________________________________________________________|

        private string lineName_st2 = "";
        private string supervisor_st2 = "";
        private double scalerRoundedWeight_st2 = 0;
        public int currentAcceptedSackWeight_st2 = 0;
        //fill this during the api fetch w/ barcode
        public int currentAcceptedLeafWeight_st2 = 0;
        private double currentGoldenLeafWeight_st2 = 0;
        private double currentNormalLeafWeight_st2 = 0;

        public double currentTotalDeduction_st2 = 0;

        //used to count the number of rounds(for the table indexing and other purposes if necessary)
        private int _currentTurn_st2 = 1;
        //finalized values by each round to send to db/api
        private float finalWeightScalerWeight_st2 = 0; //this goes as the accepted value to api
        private int finalAcceptedLeafWeight_st2 = 0;
        private int finalGoldenLeafWeight_st2 = 0;
        private int finalNormalLeafWeight_st2 = 0;

        private int finalNBoxes_st2 = 0;
        private int finalNSacks_st2 = 0;
        private int finalSackWeight_st2 = 0;

        private int finalWateredWeight_st2 = 0;
        private int finalMaturedWeight_st2 = 0;
        private int finalSpoiledWeight_st2 = 0;
        private int finalRejectedWeight_st2 = 0;

        private int finalAvailableGoldenLeafWeight_st2 = 0;
        private int finalAvailableNormalLeafWeight_st2 = 0;


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
            runtimeService.ExecuteRunExe();

        }


        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            CheckActiveStationChange();
            if (e.Key == Key.Enter)
            {
                _isEnterKey = true;
                HandleStepChange(true);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HandleStepChange(false);
                e.Handled = true;
            }
        }
        private void CheckActiveStationChange()
        {
            string currentStation = IsStation1Active() ? "st1" : IsStation2Active() ? "st2" : null;
            //MessageBox.Show(currentStation+"- "+_activeStation);
            if (currentStation != _activeStation)
            {
                _lastExecutedStep = -1;
                _previousStep = -1;
                _activeStation = currentStation;
            }
        }

        private void HandleStepChange(bool isForward)
        {
            CheckActiveStationChange();
            _previousStep = _lastExecutedStep;

            if (isForward)
            {
                _lastExecutedStep = _lastExecutedStep == -1 ? 0 : (_lastExecutedStep + 1) % TotalSteps;
            }
            else
            {
                if (_lastExecutedStep == -1)
                {
                    _lastExecutedStep = FindLastTextBoxStep();
                }
                else
                {
                    _lastExecutedStep = FindPreviousTextBoxStep(_lastExecutedStep);
                }
            }

            HandleFocusTransition();
            ExecuteStep();
        }

        private void ExecuteStep()
        {
            var control = GetStepControl(_lastExecutedStep);
            if (control == null) return;

            if (control is TextBox textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
            else if (control is Button button && _lastExecutedStep != _previousStep)
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        }

        /*        private void ExecuteStep()
                {
                    var control = GetStepControl(_lastExecutedStep);

                    if (control is TextBox textBox)
                    {
                        // Skip focus if coming from confirm button
                        if (_previousStep != 9)
                        {
                            textBox.Focus();
                            textBox.SelectAll();
                        }
                        else
                        {
                            // Explicitly focus step1 after confirm
                            barcodeTxt_st1.Focus();
                            barcodeTxt_st1.SelectAll();
                        }
                    }
                    else if (control is Button button && _previousStep != _lastExecutedStep)
                    {
                        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                        // Special handling for confirm button
                        if (_lastExecutedStep == 9)
                        {
                            // Immediately move to step1 after confirmation
                            _lastExecutedStep = -1; // Reset state
                            HandleStepChange(true); // Force move to step1
                        }
                    }
                }*/
        private void HandleButtonNavigation(object sender)
        {
            if (!_isEnterKey)
            {
                int currentStep = FindStepIndexForControl((FrameworkElement)sender);
                if (currentStep != -1)
                {
                    _lastExecutedStep = currentStep;
                    if ((sender == confirmAddRowButton_st1 && IsStation1Active()) ||
                        (sender == confirmAddRowButton_st2 && IsStation2Active()))
                    {
                        _lastExecutedStep = -1;
                    }
                }
            }
            _isEnterKey = false;
        }

        private int FindStepIndexForControl(FrameworkElement control)
        {
            int totalSteps = TotalSteps;
            for (int i = 0; i < totalSteps; i++)
            {
                if (GetStepControl(i) == control)
                    return i;
            }
            return -1;
        }
        private int FindPreviousTextBoxStep(int currentStep)
        {
            int totalSteps = TotalSteps;
            for (int i = 0; i < totalSteps; i++)
            {
                currentStep = (currentStep - 1 + totalSteps) % totalSteps;
                if (IsTextBoxStep(currentStep)) return currentStep;
            }
            return currentStep;
        }

        private int FindLastTextBoxStep()
        {
            int totalSteps = TotalSteps;
            for (int i = totalSteps - 1; i >= 0; i--)
            {
                if (IsTextBoxStep(i)) return i;
            }
            return 0;
        }

        private void HandleFocusTransition()
        {
            if (_previousStep >= 0 && IsTextBoxStep(_previousStep))
            {
                var prevControl = GetStepControl(_previousStep);
                if (prevControl is TextBox)
                {
                    prevControl.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                }
            }
        }


        private FrameworkElement GetStepControl(int stepIndex)
        {
            if (IsStation1Active())
            {
                // Keep original Station 1 configuration
                return stepIndex switch
                {
                    0 => barcodeTxt_st1,
                    1 => wieghtScalerConfirmBtn_st1,
                    2 => goldenLeafWeightTxt_st1,
                    3 => nSacksTxt_st1,
                    4 => nBoxesTxt_st1,
                    5 => wateredTxt_st1,
                    6 => maturedTxt_st1,
                    7 => spoiledTxt_st1,
                    8 => rejectedTxt_st1,
                    9 => confirmAddRowButton_st1,
                    _ => null
                };
            }
            else if (IsStation2Active())
            {
                return stepIndex switch
                {
                    0 => barcodeTxt_st2,                 // Step1: TextBox
                    1 => wieghtScalerConfirmBtn_st2,      // Step2: Button
                    2 => goldenLeafWeightTxt_st2,         // Step3: TextBox
                    3 => wateredTxt_st2,                  // Step4: TextBox
                    4 => maturedTxt_st2,                  // Step5: TextBox
                    5 => spoiledTxt_st2,                  // Step6: TextBox
                    6 => rejectedTxt_st2,                 // Step7: TextBox
                    7 => confirmAddRowButton_st2,         // Step8: Button
                    _ => null
                };
            }
            return null;
        }

        // Updated textbox/button configuration
        private bool IsTextBoxStep(int stepIndex)
        {
            if (IsStation1Active())
            {
                // Keep original Station 1 configuration
                return stepIndex switch
                {
                    0 => true,
                    1 => false,
                    2 => true,
                    3 => true,
                    4 => true,
                    5 => true,
                    6 => true,
                    7 => true,
                    8 => true,
                    9 => false,
                    _ => false
                };
            }
            else if (IsStation2Active())
            {
                return stepIndex switch
                {
                    0 => true,   // barcodeTxt_st2
                    1 => false,  // wieghtScalerConfirmBtn_st2
                    2 => true,   // goldenLeafWeightTxt_st2
                    3 => true,   // wateredTxt_st2
                    4 => true,   // maturedTxt_st2
                    5 => true,   // spoiledTxt_st2
                    6 => true,   // rejectedTxt_st2
                    7 => false,  // confirmAddRowButton_st2
                    _ => false
                };
            }
            return false;
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
            runtimeService.KillRunExe();
            this.Close();
        }

        List<LineMasterBlockModel> lineMasterData = null;
        List<String> supervisorData = null;

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update status label to indicate that fetching has started
                statusLabel.Content = "Fetching line master data...";
                if (lineMasterData == null)
                {
                    lineMasterData = await _consoleHandler.getLineMasterData();
                    if (lineMasterData != null && lineMasterData.Any())
                    {
                        foreach (var lineMaster in lineMasterData)
                        {
                            //System.Diagnostics.Debug.WriteLine($"ID: {lineMaster.id}, Line Name: {lineMaster.LineName}, Line Master: {lineMaster.LineMaster}");
                            lineNameCmb_st1.Items.Add(lineMaster.LineName);
                            lineNameCmb_st2.Items.Add(lineMaster.LineName);
                        }
                    }
                }
                // Update status label to indicate success
                statusLabel.Content = "Line master data fetched successfully.";
            }
            catch (Exception ex)
            {
                // Update status label with error information
                statusLabel.Content = $"Error fetching line master data: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error fetching line master data: {ex.Message}");
            }

            try
            {
                if (supervisorData == null)
                {
                    statusLabel.Content = "Fetching supervisor data...";
                    System.Diagnostics.Debug.WriteLine("> fetching supervisor data");
                    supervisorData = await _consoleHandler.getUsernames();
                    if (supervisorData != null && supervisorData.Any())
                    {
                        foreach (string supervisor in supervisorData)
                        {
                            supervisorCmb_st1.Items.Add(supervisor);
                            supervisorCmb_st2.Items.Add(supervisor);
                        }
                    }
                    statusLabel.Content = "Supervisor fetching done.";
                    System.Diagnostics.Debug.WriteLine("> supervisor fetching done");
                }
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Error fetching supervisor data: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error fetching supervisor data: {ex.Message}");
            }


            string email = UsernameTextBox.Text;
            string password = PasswordBoxControl.Password;
            string username = "";
            try
            {
                statusLabel.Content = "Logging in...";
                username = await _consoleHandler.loginUser(email, password);
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Login failed: {ex.Message}";
            }



            //clear customer completion table upon login
            CustomerCompletionRowPanel.Children.Clear();


            if (!username.Equals("unknown")/*true*/)
            {
                statusLabel.Content = "Login Success!";
                statusLabel.Content = "";

                // Check which radio button is selected and show the corresponding page
                if (sender is Button clickedButton)
                {
                    if (clickedButton.Name == "LoginStation1Button")
                    {
                        //load the username as the leaf weight officer
                        weightLeafOfficerTxt_st1.Text = username;

                        LoginFrame.Visibility = Visibility.Collapsed;
                        StationMainFrame.Visibility = Visibility.Visible;
                        Station1Frame.Visibility = Visibility.Visible;
                        Station2Frame.Visibility = Visibility.Collapsed;
                        //change topbar text
                        //TopBarText.Text = "වේදිකාව-1";
                        TopBarStationName.Text = "දළු කිරීම";
                        //for enter esc navigation
                        _activeStation = "Station1Frame";
                        Station1Frame.Focus();

                        //opening customer window
                        OpenCustomerWindow();
                    }
                    else if (clickedButton.Name == "LoginStation2Button")
                    {
                        //load the username as the leaf weight officer
                        weightLeafOfficerTxt_st2.Text = username;

                        LoginFrame.Visibility = Visibility.Collapsed;
                        StationMainFrame.Visibility = Visibility.Visible;
                        Station2Frame.Visibility = Visibility.Visible;
                        Station1Frame.Visibility = Visibility.Collapsed;

                        //change topbar text";
                        TopBarStationName.Text = "ගෝනි කිරීම";
                        //for enter esc navigation
                        _activeStation = "Station2Frame";
                        Station2Frame.Focus();

                        //opening customer window
                        //OpenCustomerWindow();
                    }
                    else if (clickedButton.Name == "LoginAdminButton")
                    {
                        //PageAdmin.Visibility = Visibility.Visible;
                    }
                }

                //clearing login textboxes otherwise there are visible even after a logout
                UsernameTextBox.Text = "";
                PasswordBoxControl.Password = "";
            }
            else 
            {
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
            //LoginFrame.Visibility = Visibility.Visible;
            StationMainFrame.Visibility = Visibility.Collapsed;
            Station1Frame.Visibility = Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;

            //clear existing data
            //clear weight leaf cmb officer data
            //lineNameCmb_st1.Items.Clear();
            //lineNameCmb_st2.Items.Clear();
            //clear line master cmb data
            //supervisorCmb_st1.Items.Clear();
            //supervisorCmb_st2.Items.Clear();

            //clear all 04 tables
            MemberTurnTablePanel_st1.Children.Clear();
            MemberTurnTablePanel_st2.Children.Clear();
            LineTablePanel_st1.Children.Clear();
            CustomerCompletionRowPanel.Children.Clear();

            //clear all total rows
            rowMemberTurns_st1.Text = "";
            rowGoldenLeafWeights_st1.Text = "";
            rowGreenLeafWeights_st1.Text = "";
            rowNBoxes_st1.Text = "";
            rowNSacks_st1.Text = "";
            rowTotalLeafWeights_st1.Text = "";
            finalWeightScalerWeight_st1 = 0;
            finalAcceptedLeafWeight_st1 = 0;
            finalGoldenLeafWeight_st1 = 0;
            finalNormalLeafWeight_st1 = 0;

            finalNBoxes_st1 = 0;
            finalNSacks_st1 = 0;

            finalWateredWeight_st1 = 0;
            finalMaturedWeight_st1 = 0;
            finalSpoiledWeight_st1 = 0;
            finalRejectedWeight_st1 = 0;

            finalAvailableGoldenLeafWeight_st1 = 0;
            finalAvailableNormalLeafWeight_st1 = 0;

            //clear all textboxes with helper variables
            wateredTxt_st1.Text = "";
            rejectedTxt_st1.Text = "";
            spoiledTxt_st1.Text = "";
            maturedTxt_st1.Text = "";
            currentTotalDeduction_st1 = 0;

            normalLeafWeightTxt_st1.Text = "";
            currentNormalLeafWeight_st1 = 0;

            goldenLeafWeightTxt_st1.Text = "";
            currentGoldenLeafWeight_st1 = 0;

            acceptedLeafWeightTxt_st1.Text = "";
            currentAcceptedLeafWeight_st1 = 0;
            scalerRoundedWeight_st1 = 0;

            //keep these empty else both textboxes gets disabled by logic.
            nSacksTxt_st1.Text = "";
            nBoxesTxt_st1.Text = "";

            //st2
            wateredTxt_st2.Text = "";
            rejectedTxt_st2.Text = "";
            spoiledTxt_st2.Text = "";
            maturedTxt_st2.Text = "";
            currentTotalDeduction_st2 = 0;

            normalLeafWeightTxt_st2.Text = "";
            currentNormalLeafWeight_st2 = 0;

            goldenLeafWeightTxt_st2.Text = "";
            currentGoldenLeafWeight_st2 = 0;

            acceptedLeafWeightTxt_st2.Text = "";
            currentAcceptedLeafWeight_st2 = 0;
            scalerRoundedWeight_st2 = 0;

            acceptedSackWeightTxt_st2.Text = "";
            totalNSacksTxt_st2.Text = "";
            //st2 end

            if (customerWindow != null)
            {
                customerWindow.Close();
            }
            //hmm you need either to clear all textboxes or restart the app.
            System.Diagnostics.Debug.WriteLine("system check 1");
            runtimeService.KillRunExe();
            runtimeService.KillRunExe();
            runtimeService.KillRunExe();
            System.Diagnostics.Debug.WriteLine("system check 2");
            StartupTheAppAsync();
        }


        //station1 frame_______________________________________________________________________________________________________________________

        private async void barcodeTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            string request = barcodeTxt_st1.Text;
                //String memberName = await _consoleHandler.GetMemberName(request);
                //System.Diagnostics.Debug.WriteLine(request+": "+ memberName);
                //customerNameTxt_st1.Text = memberName;
            string memberName = null;
            try
            {
                memberName = await _consoleHandler.GetMemberName(request);
                customerNameTxt_st1.Text = memberName;
                if (memberName.Equals("No name with initials found")) 
                {
                    customerNameTxt_st1.Text = "...";
                }
            }
            catch (Exception ex)
            {
                // Handle exception and notify user
                //MessageBox.Show($"Error retrieving member name: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Get Username by Barcode st1 Exception: {ex}");
            }

            //used to count the number of rounds(for the table indexing and other purposes if necessary)
            _currentTurn_st1 = 1;

            //resetting values for the next new member
            finalWeightScalerWeight_st1 = 0; 
            finalAcceptedLeafWeight_st1 = 0;
            finalGoldenLeafWeight_st1 = 0;
            finalNormalLeafWeight_st1 = 0;

            finalNBoxes_st1 = 0;
            finalNSacks_st1 = 0;

            finalWateredWeight_st1 = 0;
            finalMaturedWeight_st1 = 0;
            finalSpoiledWeight_st1 = 0;
            finalRejectedWeight_st1 = 0;

            finalAvailableGoldenLeafWeight_st1 = 0;
            finalAvailableNormalLeafWeight_st1 = 0;

            //clear member wise table data and clear it's total row
            MemberTurnTablePanel_st1.Children.Clear();
            rowNBoxes_st1.Text = "0";
            rowNSacks_st1.Text = "0";
            rowGreenLeafWeights_st1.Text = "0";
            rowGoldenLeafWeights_st1.Text = "0";
            rowTotalLeafWeights_st1.Text = "0"; //only sacks and boxes deducted
        }

        private async void lineNameCmb_st1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //clear the linewise table before entering new data
            LineTablePanel_st1.Children.Clear();

            //MessageBox.Show("jfsdlfksd");
            //System.Diagnostics.Debug.WriteLine($"fjkdfldsfdskf");
            string searchLineName = lineNameCmb_st1.SelectedItem.ToString(); // replace with the line name you're searching for
            var result = lineMasterData.FirstOrDefault(item => item.LineName == searchLineName);
            if (result != null)
            {
                //MessageBox.Show("brrrr");
                //System.Diagnostics.Debug.WriteLine($"brrrrrrrrr");
                lineMasterNameLbl_st1.Text = result.LineMaster;
                //get line wise rows by line name
                //populate the table
                //increment the total at last
                //LineTablePanel_st1.Children.Add();

                //Station1LineTableRow lr1 = new Station1LineTableRow("001", "0", "0", "0", "", "");
                //System.Diagnostics.Debug.WriteLine("This is a debug message.");

                try
                {
                var data = await _consoleHandler.getDataByFilter(result.LineName.ToString());
                    System.Diagnostics.Debug.WriteLine(result.LineName.ToString());
                //MessageBox.Show("here triggered");
                    if (data.Any())
                {
                        //MessageBox.Show("data found");
                        foreach (var transaction in data)
                    {
                            Station1LineTableRow lr1 = new Station1LineTableRow(transaction.Id.ToString(), transaction.bag_count.ToString(), transaction.box_count.ToString(), transaction.total_gold_leaf_weight.ToString(), transaction.actual_nomal_leaf_weight.ToString(), (transaction.total_gold_leaf_weight+transaction.actual_nomal_leaf_weight).ToString());
                            LineTablePanel_st1.Children.Add(lr1);
                            System.Diagnostics.Debug.WriteLine($"Transaction: Line Name: {transaction.linename}, Date: {transaction.date}, Box Count: {transaction.barcode_details}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No transactions found for the specified line name and date.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing transactions by line name and date: {ex.Message}");
            }

                //// Loop through each transaction and print details to the debug console
                //for (int i = 0; i < customerTransactions_st2.Count; i++)
                //{
                //    var transaction = customerTransactions_st2[i];
                //    //System.Diagnostics.Debug.WriteLine($"Transaction {i + 1}:");
                //    //System.Diagnostics.Debug.WriteLine($"barcode_details: {transaction.barcode_details}");
                //    //System.Diagnostics.Debug.WriteLine($"name_with_initials: {transaction.name_with_initials}");
                //    //System.Diagnostics.Debug.WriteLine($"nSacks: {transaction.bag_count}");
                //    //System.Diagnostics.Debug.WriteLine($"remaining sacks: {null}");
                //    //System.Diagnostics.Debug.WriteLine($"Total Leaf Weight: {transaction.real_value}");
                //    //System.Diagnostics.Debug.WriteLine($"accepted leaf weight: {transaction.total_leaf_weight}");
                //    //System.Diagnostics.Debug.WriteLine($"golden leaf weight: {transaction.final_gold_leaf_count}");
                //    CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), "0", transaction.real_value.ToString(), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                //    CustomerCompletionRowPanel.Children.Add(cctr4);
                //}
            }
            else
            {
                lineMasterNameLbl_st1.Text = "-";
            }
        }

        private void weightScalerStatusTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (weightScalerValTxt_st1 == null)
                return;
            if (weightScalerStatus_st1 == null)
                return;
            if (wieghtScalerConfirmBtnBorder_st1 == null)
                return;
            if (wieghtScalerConfirmBtn_st1 == null)
                return;
            if (!double.TryParse(weightScalerValTxt_st1.Text, out double scalerVal) || scalerVal < 0)
                scalerVal = 0;
            if (scalerVal > 0 || weightScalerStatus_st1.Text.Equals("සමබරයි"))
            {
                wieghtScalerConfirmBtn_st1.IsEnabled = true;
                wieghtScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
            else
            {
                wieghtScalerConfirmBtn_st1.IsEnabled = false;
                wieghtScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95D4A1"));
            }
        }
        private void wieghtScalerConfirmBtn_st1_Click(object sender, RoutedEventArgs e)
        {
            //if (!double.TryParse(nSacksTxt_st1.Text, out double nSacks) || nSacks < 0)
            //    nSacks = 0;
            //if (!double.TryParse(nBoxesTxt_st1.Text, out double nBoxes) || nBoxes < 0)
            //    nBoxes = 0;
            //if (nBoxes == 0 && nSacks == 0) 
            //{
            //    MessageBox.Show(
            //        "ගෝනි හෝ පෙට්ටි බර ඇතුලත් කරන්න",
            //        "තරාදි කියවීම",
            //        MessageBoxButton.OK,
            //        MessageBoxImage.Information
            //    );
            //    return;
            //}
            // Example weight text: "36.5KG"
            if (!double.TryParse(weightScalerValTxt_st1.Text, out double scalerWeight) || scalerWeight < 0)
                scalerWeight = 0;

            //int bagWeightLimit = 23;
            int floorValue = (int)Math.Floor(scalerWeight);
            // Check if the status equals "සමබරයි and it's not zero"
            if (weightScalerStatus_st1.Text == "සමබරයි" && scalerWeight != 0)
            {
                ////string weightText = weightScalerValTxt_st1.Text.ToUpper().Replace("KG", "").Trim();
                //if (nBoxes == 0 && nSacks != 0) {
                //    //less than 23KG per sack(default)
                    
                //    //less than 23KG per sack--> to avoid 23x0sacks = 0KG is < 35.5KG
                //    if (nSacks == 0)
                //    {
                //        floorValue = (int)Math.Floor(scalerWeight);
                //    }
                //    //more than 23KG per sack
                //    else if (floorValue > (bagWeightLimit * nSacks))
                //    {
                //        floorValue = (int)nSacks * bagWeightLimit;
                //    }
                //}
                //if (nBoxes != 0 && nSacks == 0)
                //{
                //    double boxWeights = Math.Ceiling(nBoxes * singleBoxWeight);
                //    floorValue = floorValue - (int)boxWeights;

                //}
                    // Get the largest integer less than or equal to the specified number

                    //check that rounded weight is more than 23KG per one sack for all the num. of sacks
                    //if (floorValue > (23*nSacks)) {
                    //}
                    // Update the acceptedLeafWeightTxt_st1 TextBox/TextBlock
                acceptedLeafWeightTxt_st1.Text = floorValue.ToString();
                scalerRoundedWeight_st1 = floorValue;
                currentAcceptedLeafWeight_st1 = floorValue;

                normalLeafWeightTxt_st1.Text = floorValue.ToString();
                currentNormalLeafWeight_st1 = floorValue;
                currentGoldenLeafWeight_st1 = 0;

                //to catch up with Enter key press event(in case of the manual click)
                ConsoleSound.PlayStable();

            }
            else 
            {
                MessageBox.Show(
                    "තරාදිය 0 නොවන සමබර විට කියවීම ගන්න",
                    "තරාදි කියවීම",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }


        

        //gold and normal leaf textbox logic
        private void GoldenAndNormalWeight_st1_TextChanged(object sender, TextChangedEventArgs e)
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
        private void GoldenAndNormalWeight_st1_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        private void SackDeduction_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(acceptedLeafWeightTxt_st1.Text, out double acceptedLeafWeight) || acceptedLeafWeight < 0)
                acceptedLeafWeight = 0;
            if (!double.TryParse(goldenLeafWeightTxt_st1.Text, out double goldenLeafWeight) || goldenLeafWeight < 0)
                goldenLeafWeight = 0;
            if (!double.TryParse(normalLeafWeightTxt_st1.Text, out double normalLeafWeight) || normalLeafWeight < 0)
                normalLeafWeight = 0;
            if (!double.TryParse(nSacksTxt_st1.Text, out double nSacks) || nSacks < 0)
                nSacks = 0;
            double sacksWeightLimit = nSacks * 23;

            if (nSacks != 0 && scalerRoundedWeight_st1 > sacksWeightLimit)
            {
                acceptedLeafWeightTxt_st1.Text = sacksWeightLimit.ToString();
                currentAcceptedLeafWeight_st1 = (int)sacksWeightLimit;
            }
            else
            {
                acceptedLeafWeightTxt_st1.Text = scalerRoundedWeight_st1.ToString();
                currentAcceptedLeafWeight_st1 = (int)scalerRoundedWeight_st1;
            }
            //updating normal and golden leaf weights
            if ((currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) - goldenLeafWeight >= 0) {
                normalLeafWeightTxt_st1.Text = ((currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) - goldenLeafWeight).ToString();
                //update helper variables
                currentGoldenLeafWeight_st1 = goldenLeafWeight;
                currentNormalLeafWeight_st1 = (currentAcceptedLeafWeight_st1 + currentTotalDeduction_st1) - goldenLeafWeight;
            }
            else
            {
                normalLeafWeightTxt_st1.Text = "0";
                currentNormalLeafWeight_st1 = 0;
                //update helper variables
                //MessageBox.Show(""+currentAcceptedLeafWeight_st1+"-"+currentTotalDeduction_st1);
                //goldenLeafWeightTxt_st1.Text = (currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1).ToString();
                //currentGoldenLeafWeight_st1 = currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1;
                goldenLeafWeightTxt_st1.Text = "0";
                currentGoldenLeafWeight_st1 = 0;
                //SHOULD i also zero the rounded value? i guess not?
                //wateredTxt_st1.Text = "0";
                //spoiledTxt_st1.Text = "0";
                //maturedTxt_st1.Text = "0";
                //rejectedTxt_st1.Text = "0";
                currentTotalDeduction_st1 = 0;
            }
            //for testing purposes
            greenText.Text = currentNormalLeafWeight_st1.ToString();
            goldText.Text = currentGoldenLeafWeight_st1.ToString();
        }

        private void BoxDeduction_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(acceptedLeafWeightTxt_st1.Text, out double acceptedLeafWeight) || acceptedLeafWeight < 0)
                acceptedLeafWeight = 0;
            if (!double.TryParse(goldenLeafWeightTxt_st1.Text, out double goldenLeafWeight) || goldenLeafWeight < 0)
                goldenLeafWeight = 0;
            if (!double.TryParse(normalLeafWeightTxt_st1.Text, out double normalLeafWeight) || normalLeafWeight < 0)
                normalLeafWeight = 0;
            if (!double.TryParse(nBoxesTxt_st1.Text, out double nBoxes) || nBoxes < 0)
                nBoxes = 0;
            double boxWeights = Math.Ceiling(nBoxes * singleBoxWeight);

            //exceed limit validation
            if (scalerRoundedWeight_st1 !> boxWeights)
            //{
            //    acceptedLeafWeightTxt_st1.Text = (scalerRoundedWeight_st1 - boxWeights).ToString();
            //    currentAcceptedLeafWeight_st1 = (int)(scalerRoundedWeight_st1 - boxWeights);
            //}
            //else
            {
                acceptedLeafWeightTxt_st1.Text = (scalerRoundedWeight_st1 - boxWeights).ToString();
                currentAcceptedLeafWeight_st1 = (int)(scalerRoundedWeight_st1 - boxWeights);
                //MessageBox.Show("nboxes exceeds rounded weights");
            }
            //updating normal and golden leaf weights
            if ((currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) - goldenLeafWeight >= 0)
            {
                normalLeafWeightTxt_st1.Text = ((currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) - goldenLeafWeight).ToString();
                //update helper variables
                currentGoldenLeafWeight_st1 = goldenLeafWeight;
                currentNormalLeafWeight_st1 = (currentAcceptedLeafWeight_st1 + currentTotalDeduction_st1) - goldenLeafWeight;
            }
            else
            {
                normalLeafWeightTxt_st1.Text = "0";
                currentNormalLeafWeight_st1 = 0;
                //update helper variables
                //MessageBox.Show("" + currentAcceptedLeafWeight_st1 + "-" + currentTotalDeduction_st1);
                goldenLeafWeightTxt_st1.Text = (currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1).ToString();
                currentGoldenLeafWeight_st1 = currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1;
            }
            //for testing purposes
            greenText.Text = currentNormalLeafWeight_st1.ToString();
            goldText.Text = currentGoldenLeafWeight_st1.ToString();
        }

        //weight deduction logic
        private void WeightDeduction_st1_TextChanged(object sender, TextChangedEventArgs e)
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
            //if (!double.TryParse(nBoxesTxt_st1.Text, out double nBoxes) || nBoxes < 0)
            //    nBoxes = 0;
            //double boxWeights = Math.Ceiling(nBoxes * singleBoxWeight);

            //Calculate total
            double totalDeductions = watered + rejected + matured + spoiled /*+ boxWeights*/;

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
        private void WeightDeduction_st1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block non-digit characters
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }
            //to avoid having minus values if leaf weights are zero
            if (/*goldenLeafWeightTxt_st1.Text.Equals("") ||*/ normalLeafWeightTxt_st1.Text.Equals("") || acceptedLeafWeightTxt_st1.Text.Equals("")) 
            {
                MessageBox.Show(
                        "කරුණාකර සාමාජික දළු බර ඇතුලත් කරන්න",
                        "ගෝනි බර ඇතුලත් කිරීම",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                e.Handled = true;
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









        private void BoxWeightDeduction_st1_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

            //Check if the proposed text is a valid integer and THE TOTAL BOX WIEGHT(num of boxes* single box weight) doesn't exceeds total weight old logic
            if (int.TryParse(proposedText, out int inputNumber) && (Math.Ceiling(inputNumber * singleBoxWeight)) > currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1)
            {
                e.Handled = true; // Block the input
                //MessageBox.Show((Math.Ceiling(inputNumber * singleBoxWeight)).ToString() + " Deduction Error: exceeds golden and normal leaf(" + (currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) + ") weight.");
            }
        }

        private void nSacksTxt_st1_PreviewTextInput(object sender, TextCompositionEventArgs e)
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



        //clear button st1
        private void clearBtn_st1_Clicked(object sender, RoutedEventArgs e)
        {
            wateredTxt_st1.Text = "";
            rejectedTxt_st1.Text = "";
            spoiledTxt_st1.Text = "";
            maturedTxt_st1.Text = "";
            currentTotalDeduction_st1 = 0;

            normalLeafWeightTxt_st1.Text = "";
            currentNormalLeafWeight_st1 = 0;

            goldenLeafWeightTxt_st1.Text = "";
            currentGoldenLeafWeight_st1 = 0;

            acceptedLeafWeightTxt_st1.Text = "";
            currentAcceptedLeafWeight_st1 = 0;
            scalerRoundedWeight_st1 = 0;

            //keep these empty else both textboxes gets disabled by logic.
            nSacksTxt_st1.Text = "";
            nBoxesTxt_st1.Text = "";

        }


        private async void confirmAddRowButton_st1_Click(object sender, RoutedEventArgs e)
        {
            //preparing values for the finish api command
            //lineName_st1 = lineNameCmb_st1.SelectedValue.ToString();
            if (lineNameCmb_st1.SelectedValue != null)
            {
                lineName_st1 = lineNameCmb_st1.SelectedValue.ToString();
            }
            else
            {
                // Handle the case when no item is selected.
                // For example, assign a default value or display an error message.
                lineName_st1 = ""; // or any appropriate default
            }
            //supervisor_st1 = supervisorCmb_st1.SelectedValue.ToString();
            if (supervisorCmb_st1.SelectedValue != null)
            {
                supervisor_st1 = supervisorCmb_st1.SelectedValue.ToString();
            }
            else
            {
                supervisor_st1 = "";
            }

            if (lineName_st1.Equals("") || supervisor_st1.Equals("")) 
            {
                MessageBox.Show("සාමාජික අංකය හා ප්‍රවාහන මාර්හය ඇතුලත් කරන්න");
                return;
            }

            
            //load table rows(test) after successful update
            if (!acceptedLeafWeightTxt_st1.Text.Equals("") && !normalLeafWeightTxt_st1.Text.Equals("") && ((!nSacksTxt_st1.Text.Equals("") && nBoxesTxt_st1.Text.Equals("")) || (nSacksTxt_st1.Text.Equals("") && !nBoxesTxt_st1.Text.Equals(""))))
            {
                //these are used to both updating the db and incrementing total valles for the table
                float.TryParse(weightScalerValTxt_st1.Text, out float weightScalerValue);
                int.TryParse(acceptedLeafWeightTxt_st1.Text, out int acceptedLeafWeight);

                int.TryParse(nBoxesTxt_st1.Text, out int nBoxes);
                int.TryParse(nSacksTxt_st1.Text, out int nSacks);

                int.TryParse(wateredTxt_st1.Text, out int wateredWeight);
                int.TryParse(maturedTxt_st1.Text, out int maturedWeight);
                int.TryParse(spoiledTxt_st1.Text, out int spoiledWeight);
                int.TryParse(rejectedTxt_st1.Text, out int rejectedWeight);

                int.TryParse(normalLeafWeightTxt_st1.Text, out int availableNormalLeafWeight);
                int.TryParse(goldenLeafWeightTxt_st1.Text, out int availableGoldenLeafWeight);
                bool _isSuccess = false;
                try
                {
                    var newTransaction = new TransactionLogBlockModel
                    {
                        linename = lineName_st1,
                        transportagent = lineMasterNameLbl_st1.Text,
                        company = "නව ඇලන්වැලි තේ කම්හල",
                        leaf_weight_officer = weightLeafOfficerTxt_st1.Text,
                        superviosr = supervisor_st1,
                        barcode_details = barcodeTxt_st1.Text,
                        name_with_initials = customerNameTxt_st1.Text,
                        phone_number = "123-456-7890",
                        date = DateTime.Now.ToString("yyyy-MM-dd"),
                        box_count = nBoxes,
                        bag_count = nSacks,
                        real_value = weightScalerValue,
                        maximum_nomal_leaf_weight = 23,
                        total_leaf_weight = (int)currentNormalLeafWeight_st1 + (int)currentGoldenLeafWeight_st1,
                        actual_nomal_leaf_weight = (int)currentNormalLeafWeight_st1,
                        total_gold_leaf_weight = (int)currentGoldenLeafWeight_st1,
                        water = wateredWeight,
                        morapuwata = maturedWeight,
                        thambimata = spoiledWeight,
                        reject = rejectedWeight,
                        box_weight = (int)Math.Ceiling(finalNBoxes_st1 * singleBoxWeight),
                        final_green_leaf_count = availableNormalLeafWeight,
                        final_gold_leaf_count = availableGoldenLeafWeight
                    };

                    System.Diagnostics.Debug.WriteLine(newTransaction);

                    // Call the service to add the transaction
                    _isSuccess = await _consoleHandler.AddTransactionAsync(newTransaction);
                }
                catch (Exception ex)
                {
                    // Handle exception here
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
                }
                if (_isSuccess)
                {
                    int totalWeight = (availableGoldenLeafWeight + availableNormalLeafWeight);
                    Station1TableRow station1TableRow1 = new Station1TableRow(_currentTurn_st1++.ToString(), nSacksTxt_st1.Text, nBoxesTxt_st1.Text, normalLeafWeightTxt_st1.Text, goldenLeafWeightTxt_st1.Text, /*totalWeight.ToString()*/acceptedLeafWeightTxt_st1.Text);
                    // When adding a new row dynamically(no need now)
                    //int insertIndex = MemberTurnTablePanel_st1.Children.Count - 1;
                    MemberTurnTablePanel_st1.Children.Add(station1TableRow1);
                    //MemberTurnTablePanel_st1.Children.Add(station1TableRow1);



                    finalWeightScalerWeight_st1 += weightScalerValue;
                    finalAcceptedLeafWeight_st1 += acceptedLeafWeight;

                    finalNormalLeafWeight_st1 += (int)currentNormalLeafWeight_st1;
                    finalGoldenLeafWeight_st1 += (int)currentGoldenLeafWeight_st1;

                    finalNBoxes_st1 += nBoxes;
                    finalNSacks_st1 += nSacks;

                    finalWateredWeight_st1 += wateredWeight;
                    finalMaturedWeight_st1 += maturedWeight;
                    finalSpoiledWeight_st1 += spoiledWeight;
                    finalRejectedWeight_st1 += rejectedWeight;

                    finalAvailableGoldenLeafWeight_st1 += availableGoldenLeafWeight;
                    finalAvailableNormalLeafWeight_st1 += availableNormalLeafWeight;

                    //if everything was added successfully, assign updated final values to the total values row
                    //rowMemberTurns_st1.Text = MemberTurnTablePanel_st1.Children.Count.ToString();
                    rowNBoxes_st1.Text = finalNBoxes_st1.ToString();
                    rowNSacks_st1.Text = finalNSacks_st1.ToString();
                    rowGreenLeafWeights_st1.Text = finalAvailableNormalLeafWeight_st1.ToString();
                    rowGoldenLeafWeights_st1.Text = finalAvailableGoldenLeafWeight_st1.ToString();
                    rowTotalLeafWeights_st1.Text = finalAcceptedLeafWeight_st1.ToString(); //only sacks and boxes deducted
                                                                                           //System.Diagnostics.Debug.WriteLine("Added value: "+ finalWeightScalerWeight_st1);

                    //if everything was added successfully, clear all textboxes(except table row, member & line master/name details)
                    nSacksTxt_st1.Text = "";
                    nBoxesTxt_st1.Text = "";

                    wateredTxt_st1.Text = "";
                    rejectedTxt_st1.Text = "";
                    spoiledTxt_st1.Text = "";
                    maturedTxt_st1.Text = "";

                    normalLeafWeightTxt_st1.Text = "";
                    goldenLeafWeightTxt_st1.Text = "";
                    acceptedLeafWeightTxt_st1.Text = "";

                    currentTotalDeduction_st1 = 0;
                    currentNormalLeafWeight_st1 = 0;
                    currentGoldenLeafWeight_st1 = 0;
                    currentAcceptedLeafWeight_st1 = 0;

                    //handle next enter press step after successful execution
                    HandleButtonNavigation(sender);
                }
                else {
                    MessageBox.Show("Update failed.");
                }

                try
                {
                    bool _isoks = await _consoleHandler.verifyTransactionsCloudCheck();
                    if (_isoks)
                    {
                        System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked done! ]:::::::::::::::");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked failed! ]::::::::::::");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("කරුණාකර සියලු තොරතුරු අතුලත් කරන්න");
            }
        }

        //station2 frame______________________________________________________________________________________________________________________________

        private async void barcodeTxt_st2_TextChanged(object sender, TextChangedEventArgs e)
        {
            //clear member turn table for the next member(this is hidden currently)
            MemberTurnTablePanel_st2.Children.Clear();

                String memberName = await _consoleHandler.GetMemberName(barcodeTxt_st2.Text);

                System.Diagnostics.Debug.WriteLine(barcodeTxt_st2 + ": " + memberName);
                customerNameTxt_st2.Text = memberName;

                var memberDetails = await _consoleHandler.GetTransactionData(barcodeTxt_st2.Text,"");

                if (memberDetails!=null) {
                    //load and populate additional data like previous leaf data, box data like stuff
                    //tbd for transport route & agent
                    lineNameCmb_st2.SelectedItem = memberDetails.linename;
                    lineMasterNameLbl_st2.Text = memberDetails.transportagent;
                    //follow steps when inserting values to avoid collisions
                    totalNSacksTxt_st2.Text = memberDetails.bag_count.ToString();

                    //update the current values

                    maturedTxt_st2.Text = memberDetails.morapuwata.ToString();
                    wateredTxt_st2.Text = memberDetails.water.ToString();
                    spoiledTxt_st2.Text = memberDetails.thambimata.ToString();
                    rejectedTxt_st2.Text = memberDetails.reject.ToString();

                    acceptedLeafWeightTxt_st2.Text = memberDetails.actual_nomal_leaf_weight.ToString();
                    normalLeafWeightTxt_st2.Text = memberDetails.final_green_leaf_count.ToString();
                    goldenLeafWeightTxt_st2.Text = memberDetails.final_gold_leaf_count.ToString();

                    currentAcceptedLeafWeight_st2 = memberDetails.actual_nomal_leaf_weight;
                    currentNormalLeafWeight_st2 = memberDetails.final_green_leaf_count;
                    currentGoldenLeafWeight_st2 = memberDetails.final_gold_leaf_count;
                    currentTotalDeduction_st2 = memberDetails.morapuwata+memberDetails.water+memberDetails.reject+memberDetails.thambimata;

                //MessageBox.Show(currentAcceptedLeafWeight_st2 + "= " + currentNormalLeafWeight_st2 + " + " + currentGoldenLeafWeight_st2 + "| total deduction: "+currentTotalDeduction_st2);
                }
                //.Text = "";
                //.Text = "";
                //public int currentAcceptedLeafWeight_st2 = 82;
                //private double currentGoldenLeafWeight_st2 = 0;
                //private double currentNormalLeafWeight_st2 = 0;

                //public double currentTotalDeduction_st2 = 0;

        }

        private async void lineNameCmb_st2_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

            //clear(refresh) & repopulate the customer completion table
            string lineName = "";
            if (lineNameCmb_st2.SelectedValue != null)
            {
                lineName = lineNameCmb_st2.SelectedValue.ToString();
            }
            else
            {
                lineName = "";
            }
            try
            {
                CustomerCompletionRowPanel.Children.Clear();
                var customerTransactions_st2 = await _consoleHandler.GetTransactionData(lineName);
                for (int i = 0; i < customerTransactions_st2.Count; i++)
                {
                    var transaction = customerTransactions_st2[i];
                    //System.Diagnostics.Debug.WriteLine($"Transaction {i + 1}:");
                    CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), "0", transaction.real_value.ToString(), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                    CustomerCompletionRowPanel.Children.Add(cctr4);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("customer completion tbl population: " + ex.Message);
            }


        }

        private void weightScalerStatusTxt_st2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (weightScalerValTxt_st2 == null)
                return;
            if (weightScalerStatus_st2 == null)
                return;
            if (wieghtScalerConfirmBtnBorder_st2 == null)
                return;
            if (wieghtScalerConfirmBtn_st2 == null)
                return;
            if (!double.TryParse(weightScalerValTxt_st2.Text, out double scalerVal) || scalerVal < 0)
                scalerVal = 0;
            if (scalerVal > 0 || weightScalerStatus_st2.Text.Equals("සමබරයි"))
            {
                wieghtScalerConfirmBtn_st2.IsEnabled = true;
                wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
            else
            {
                wieghtScalerConfirmBtn_st2.IsEnabled = false;
                wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95D4A1"));
            }
        }

            
        private void weightScalerConfirmBtn_st2_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(weightScalerValTxt_st2.Text, out double scalerWeight) || scalerWeight < 0)
                scalerWeight = 0;

            //int bagWeightLimit = 23;
            int ceilingValue = (int)Math.Ceiling(scalerWeight);
            // Check if the status equals "සමබරයි and it's not zero"
            if (weightScalerStatus_st2.Text == "සමබරයි" && scalerWeight != 0)
            {
                acceptedSackWeightTxt_st2.Text = ceilingValue.ToString();
                scalerRoundedWeight_st2 = ceilingValue;
                currentAcceptedSackWeight_st2 = ceilingValue;
                // Get the largest integer less than or equal to the specified number
                //            int CeilingValue = (int)Math.Ceiling((double)weight);
                //            // Update the acceptedLeafWeightTxt_st1 TextBox/TextBlock
                //            acceptedSackWeightTxt_st2.Text = CeilingValue.ToString();
                //            currentAcceptedSackWeightTxt_st2 = CeilingValue;

                //to catch up with Enter key press event(in case of the manual click)
                ConsoleSound.PlayStable();
            }
            else
            {
                MessageBox.Show(
                    "තරාදිය 0 නොවන සමබර විට කියවීම ගන්න",
                    "තරාදි කියවීම",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }

        }

        //gold and normal leaf textbox logic
        private void GoldenAndNormalWeight_st2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (userTyped) 
            {
            // Check if controls exist (avoids NullReferenceException during initialization)
            if (acceptedLeafWeightTxt_st2 == null)
                return;
            // Parse values (handle empty/invalid input)
            if (!double.TryParse(acceptedLeafWeightTxt_st2.Text, out double acceptedLeafWeight) || acceptedLeafWeight < 0)
                acceptedLeafWeight = 0;
            if (!double.TryParse(goldenLeafWeightTxt_st2.Text, out double goldenLeafWeight) || goldenLeafWeight < 0)
                goldenLeafWeight = 0;
            if (!double.TryParse(normalLeafWeightTxt_st2.Text, out double normalLeafWeight) || normalLeafWeight < 0)
                normalLeafWeight = 0;

            //calculate total leaf
            //double total = normalLeafWeight + goldenLeafWeight;

            //available weights for golden and normal leaf weights after the deductions
            //double availableWeight = total - currentTotalDeduction_st1;

            if (goldenLeafWeightTxt_st2.Text.Equals("0") && normalLeafWeightTxt_st2.Text.Equals("0")) { return; }

            //if golden leaf weight was changed
            if (goldenLeafWeightTxt_st2.IsKeyboardFocused)
            {
                //MessageBox.Show("goldleaf: event triggered.");
                normalLeafWeightTxt_st2.Text = ((acceptedLeafWeight - currentTotalDeduction_st2) - goldenLeafWeight).ToString();
                //update helper variables
                currentGoldenLeafWeight_st2 = goldenLeafWeight;
                currentNormalLeafWeight_st2 = (currentAcceptedLeafWeight_st2 - currentTotalDeduction_st2) - goldenLeafWeight;  //this was here: (currentAcceptedLeafWeight_st2 - currentTotalDeduction_st2)
            }

            //for testing purposes
            greenText2.Text = currentNormalLeafWeight_st2.ToString();
            goldText2.Text = currentGoldenLeafWeight_st2.ToString();

        }
        }

        //to block invalid user inputs
        private void GoldenAndNormalWeight_st2_PreviewTextInput(object sender, TextCompositionEventArgs e)
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
            if (int.TryParse(proposedText, out int inputNumber) && inputNumber > currentAcceptedLeafWeight_st2)
            {
                e.Handled = true; // Block the input
                MessageBox.Show(proposedText + " exceeds Accepted leaf weight.");
            }

        }

        //weight deduction logic
        private void WeightDeduction_st2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (userTyped)
            { 
            // Check if controls exist (avoids NullReferenceException during initialization)
            if (wateredTxt_st2 == null || rejectedTxt_st2 == null || maturedTxt_st2 == null || spoiledTxt_st2 == null || currentNormalLeafWeight_st2 == null || acceptedSackWeightTxt_st2 == null)
                return;
            // Parse values (handle empty/invalid input)
            if (!double.TryParse(wateredTxt_st2.Text, out double watered) || watered < 0)
                watered = 0;
            if (!double.TryParse(rejectedTxt_st2.Text, out double rejected) || rejected < 0)
                rejected = 0;
            if (!double.TryParse(maturedTxt_st2.Text, out double matured) || matured < 0)
                matured = 0;

                //MessageBox.Show("matured: "+matured);
            if (!double.TryParse(spoiledTxt_st2.Text, out double spoiled) || spoiled < 0)
                spoiled = 0;
            //if (!double.TryParse(nBoxesTxt_st2.Text, out double nBoxes) || spoiled < 0)
            //    nBoxes = 0;
            if (!double.TryParse(acceptedSackWeightTxt_st2.Text, out double acceptedSackWeight) || acceptedSackWeight < 0)
                acceptedSackWeight = 0;
            //double boxWeights = nBoxes * singleBoxWeight;
            // Calculate total
            double totalDeductions = watered + rejected + matured + spoiled + acceptedSackWeight;

            //choose deduction type between green leaves or golden leaves based on total
            //if normal weight doesn't exceeds total deduction(no need to update golden leaf weights)
            if (totalDeductions <= currentNormalLeafWeight_st2)
            {
                normalLeafWeightTxt_st2.Text = (currentNormalLeafWeight_st2 - totalDeductions).ToString();
                goldenLeafWeightTxt_st2.Text = currentGoldenLeafWeight_st2.ToString();
                //update helper value
                currentTotalDeduction_st2 = totalDeductions;
                blueText2.Text = currentTotalDeduction_st2.ToString();
            }
            //if normal weight doesn't exceeds total deduction(now you need to update both golden leaf weights & normal leaf weights)
            else if (totalDeductions > currentNormalLeafWeight_st2)
            {
                normalLeafWeightTxt_st2.Text = "0";
                goldenLeafWeightTxt_st2.Text = (currentGoldenLeafWeight_st2 - (totalDeductions - currentNormalLeafWeight_st2)).ToString();
                //update helper value
                currentTotalDeduction_st2 = totalDeductions;
                blueText2.Text = currentTotalDeduction_st2.ToString();
            }
        }
        }

        //to block invalid user inputs for weight deduction
        private void WeightDeduction_st2_PreviewTextInput(object sender, TextCompositionEventArgs e)
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
            if (int.TryParse(proposedText, out int inputNumber) && inputNumber > currentAcceptedLeafWeight_st2 - currentTotalDeduction_st2)
            {
                e.Handled = true; // Block the input
                //MessageBox.Show(proposedText + " Deduction Error: exceeds golden and normal leaf(" + (currentAcceptedLeafWeight_st2 - currentTotalDeduction_st2) + ") weight.");
            }
        }

        private void SacksWeightDeduction_st2_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block non-digit characters
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }
            //Get the proposed new text(current text + new input)
            var textBox = (TextBox)sender;
            string proposedText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength) + e.Text;

            //Check if the proposed text is a valid integer and doesn't exceeds total weight
            if (int.TryParse(proposedText, out int inputNumber) && inputNumber > currentAcceptedLeafWeight_st2 - currentTotalDeduction_st2)
            {
                e.Handled = true; // Block the input
                //MessageBox.Show(inputNumber.ToString() + " Deduction Error: This sacks weight exceeds golden and normal leaf(" + (currentAcceptedLeafWeight_st2 - currentTotalDeduction_st2) + ") weight.");
            }
        }

        //clear button st1
        //private void clearBtn_st2_Clicked(object sender, RoutedEventArgs e)
        //{
        //    wateredTxt_st2.Text = "0";
        //    rejectedTxt_st2.Text = "0";
        //    spoiledTxt_st2.Text = "0";
        //    maturedTxt_st2.Text = "0";
        //    normalLeafWeightTxt_st2.Text = "0";
        //    goldenLeafWeightTxt_st2.Text = "0";
        //    //keep these empty else both textboxes gets disabled by logic.
        //    nSacksTxt_st1.Text = "";
        //    nBoxesTxt_st1.Text = "";
        //}

        //clear button st2
        private void clearBtn_st2_Clicked(object sender, RoutedEventArgs e)
        {
            wateredTxt_st2.Text = "";
            rejectedTxt_st2.Text = "";
            spoiledTxt_st2.Text = "";
            maturedTxt_st2.Text = "";
            currentTotalDeduction_st2 = 0;

            normalLeafWeightTxt_st2.Text = "";
            currentNormalLeafWeight_st2 = 0;

            goldenLeafWeightTxt_st2.Text = "";
            currentGoldenLeafWeight_st2 = 0;

            acceptedLeafWeightTxt_st2.Text = "";
            currentAcceptedLeafWeight_st2 = 0;
            scalerRoundedWeight_st2 = 0;

            acceptedSackWeightTxt_st2.Text = "";
            totalNSacksTxt_st2.Text = "";

        }


        //private int _currentTurn_st2 = 1;
        private async void confirmAddRowButton_st2_Click(object sender, RoutedEventArgs e)
        {
            //preparing values for the finish api command
            //lineName_st1 = lineNameCmb_st1.SelectedValue.ToString();
            if (lineNameCmb_st2.SelectedValue != null)
            {
                lineName_st2 = lineNameCmb_st2.SelectedValue.ToString();
            }
            else
            {
                // Handle the case when no item is selected.
                // For example, assign a default value or display an error message.
                lineName_st2 = ""; // or any appropriate default
            }
            //supervisor_st1 = supervisorCmb_st1.SelectedValue.ToString();
            if (supervisorCmb_st2.SelectedValue != null)
            {
                supervisor_st2 = supervisorCmb_st2.SelectedValue.ToString();
            }
            else
            {
                // Handle the case when no item is selected.
                // For example, assign a default value or display an error message.
                supervisor_st2 = ""; // or any appropriate default
            }

            if (lineName_st2.Equals("") || supervisor_st2.Equals(""))
            {
                MessageBox.Show("සාමාජික අංකය හා අධීක්ෂණ නිලධාරී ඇතුලත් කරන්න");
                return;
            }

            //load table rows(test)
            if (lineNameCmb_st2.SelectedValue != null && !barcodeTxt_st2.Text.Equals("") && !acceptedSackWeightTxt_st2.Text.Equals("") && !acceptedLeafWeightTxt_st2.Text.Equals(""))
            {
                int totalWeight = (Convert.ToInt32(goldenLeafWeightTxt_st2.Text) + Convert.ToInt32(normalLeafWeightTxt_st2.Text));

                Station1TableRow station1TableRow1 = new Station1TableRow(_currentTurn_st2++.ToString(), totalNSacksTxt_st2.Text, "", totalWeight.ToString(), goldenLeafWeightTxt_st2.Text, normalLeafWeightTxt_st2.Text);
                MemberTurnTablePanel_st2.Children.Add(station1TableRow1);

                float.TryParse(weightScalerValTxt_st2.Text, out float finalWeightScalerValue);
                int.TryParse(acceptedLeafWeightTxt_st2.Text, out int finalAcceptedLeafWeight);
                int.TryParse(acceptedSackWeightTxt_st2.Text, out int finalAcceptedSackWeight);

                int.TryParse(totalNSacksTxt_st2.Text, out int finalNSacks);

                int.TryParse(wateredTxt_st2.Text, out int finalWateredWeight);
                int.TryParse(maturedTxt_st2.Text, out int finalMaturedWeight);
                int.TryParse(spoiledTxt_st2.Text, out int finalSpoiledWeight);
                int.TryParse(rejectedTxt_st2.Text, out int finalRejectedWeight);

                int.TryParse(normalLeafWeightTxt_st2.Text, out int finalAvailableNormalLeafWeight);
                int.TryParse(goldenLeafWeightTxt_st2.Text, out int finalAvailableGoldenLeafWeight);

                if (lineNameCmb_st2.SelectedValue != null)
                {
                    lineName_st2 = lineNameCmb_st2.SelectedValue.ToString();
                }
                else
                {
                    // Handle the case when no item is selected.
                    // For example, assign a default value or display an error message.
                    lineName_st2 = ""; // or any appropriate default
                }
                //supervisor_st1 = supervisorCmb_st1.SelectedValue.ToString();
                if (supervisorCmb_st2.SelectedValue != null)
                {
                    supervisor_st2 = supervisorCmb_st2.SelectedValue.ToString();
                }
                else
                {
                    // Handle the case when no item is selected.
                    // For example, assign a default value or display an error message.
                    supervisor_st2 = ""; // or any appropriate default
                }

                //System.Diagnostics.Debug.WriteLine("Added value: "+ finalWeightScalerWeight_st1);

                //decrement the remaining sacks
                if (!int.TryParse(totalNSacksTxt_st2.Text, out int totalNSacks) || totalNSacks < 0)
                    totalNSacks = 0;

                //re-feed the updated values and helper variables to the same textboxes if there are sacks remaining


                //if everything was added successfully(and remaining sacks are zero), clear all textboxes(except table row, member & line master/name details)
                //(added later)


                bool _isdone = false;
                try
                {
                    var Finaltransaction = new FinalTransactionBlockModel
                    {
                        //linename = lineNameCmb_st2.SelectedValue.ToString(),
                        linename = lineName_st2,
                        transportagent = lineMasterNameLbl_st2.Text,
                        company = "නව ඇලන්වැලි තේ කම්හල",
                        leaf_weight_officer = weightLeafOfficerTxt_st2.Text,
                        //superviosr = supervisorCmb_st2.SelectedValue.ToString(),
                        superviosr = supervisor_st2,
                        barcode_details = barcodeTxt_st2.Text,
                        name_with_initials = customerNameTxt_st2.Text,
                        phone_number = "0712345678",
                        date = DateTime.Now.ToString("yyyy-MM-dd"),

                        bag_count = finalNSacks,

                        maximum_nomal_leaf_weight = 90,
                        total_leaf_weight = finalAcceptedLeafWeight,
                        actual_nomal_leaf_weight = (int)currentNormalLeafWeight_st1,
                        total_gold_leaf_weight = (int)currentGoldenLeafWeight_st1,

                        water = finalWateredWeight,
                        morapuwata = finalMaturedWeight,
                        thambimata = finalSpoiledWeight,
                        reject = finalRejectedWeight,

                        bag_weight = finalAcceptedSackWeight,

                        final_green_leaf_count = finalAvailableNormalLeafWeight,
                        final_gold_leaf_count = finalAvailableGoldenLeafWeight,
                        real_value = finalWeightScalerValue //TBDDD****************************************************************************
                    };

                    _isdone = await _consoleHandler.AddFinalTransactionAsync(Finaltransaction);
                }
                catch (Exception ex) {
                    MessageBox.Show("Upload Failed: "+ex.Message);
                }

                bool failed = false;
                //verifying missing transactions and replacing them
                try
                {
                    bool _isoks = await _consoleHandler.verifyTransactionsCloudCheck();
                    if (_isoks)
                    {
                        System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked done! ]::::::::::::");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked failed! ]::::::::::::");
                    }
                    failed = !_isoks;
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


                if (_isdone)
                {
                    //MessageBox.Show("upload success,");
                    System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ *************** ]::::::::::::::::::::");
                    wateredTxt_st2.Text = "";
                    rejectedTxt_st2.Text = "";
                    spoiledTxt_st2.Text = "";
                    maturedTxt_st2.Text = "";
                    currentTotalDeduction_st2 = 0;

                    normalLeafWeightTxt_st2.Text = "";
                    currentNormalLeafWeight_st2 = 0;

                    goldenLeafWeightTxt_st2.Text = "";
                    currentGoldenLeafWeight_st2 = 0;

                    acceptedLeafWeightTxt_st2.Text = "";
                    currentAcceptedLeafWeight_st2 = 0;
                    scalerRoundedWeight_st2 = 0;

                    acceptedSackWeightTxt_st2.Text = "";
                    totalNSacksTxt_st2.Text = "";
                }
                else 
                {
                    MessageBox.Show("upload failed... please try again,");
                }

                /*      public int currentAcceptedSackWeightTxt_st2 = 0;
                        //fill this during the api fetch w/ barcode
                        public int currentAcceptedLeafWeight_st2 = 82;
                        private double currentGoldenLeafWeight_st2 = 0;
                        private double currentNormalLeafWeight_st2 = 0;

                        public double currentTotalDeduction_st2 = 0;

                        //used to count the number of rounds(for the table indexing and other purposes if necessary)
                        private int _currentTurn_st2 = 1;
                        //finalized values by each round to send to db/api
                        private float finalWeightScalerWeight_st2 = 0; //this goes as the accepted value to api
                        private int finalAcceptedLeafWeight_st2 = 0;
                        private int finalGoldenLeafWeight_st2 = 0;
                        private int finalNormalLeafWeight_st2 = 0;

                        private int finalNBoxes_st2 = 0;
                        private int finalNSacks_st2 = 0;

                        private int finalWateredWeight_st2 = 0;
                        private int finalMaturedWeight_st2 = 0;
                        private int finalSpoiledWeight_st2 = 0;
                        private int finalRejectedWeight_st2 = 0;

                        private int finalAvailableGoldenLeafWeight_st2 = 0;
                        private int finalAvailableNormalLeafWeight_st2 = 0;*/

                //scalingNSacksTxt_st2.Text = "";
                //nBoxesTxt_st2.Text = "";

                //wateredTxt_st2.Text = "";
                //rejectedTxt_st2.Text = "";
                //spoiledTxt_st2.Text = "";
                //maturedTxt_st2.Text = "";

                //normalLeafWeightTxt_st2.Text = "";
                //goldenLeafWeightTxt_st2.Text = "";
                //acceptedLeafWeightTxt_st2.Text = "";
                //acceptedSackWeightTxt_st2.Text = "";

                //update the customer completion table after an update based on Line Name
                string lineName = "";
                if (lineNameCmb_st2.SelectedValue != null)
                {
                    lineName = lineNameCmb_st2.SelectedValue.ToString();
                }
                else
                {
                    lineName = "";
                }
                try
                {
                    CustomerCompletionRowPanel.Children.Clear();
                    var customerTransactions_st2 = await _consoleHandler.GetTransactionData(lineName);
                    for (int i = 0; i < customerTransactions_st2.Count; i++)
                    {
                        var transaction = customerTransactions_st2[i];
                        //System.Diagnostics.Debug.WriteLine($"Transaction {i + 1}:");
                        CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), "0", transaction.real_value.ToString(), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                        CustomerCompletionRowPanel.Children.Add(cctr4);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("customer completion tbl population: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("සියලු තො රතුරු අතුලත් කරන්න");
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

        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {

        }

        //protected override void OnClosed(EventArgs e)
        //{
        //    runtimeService.OnWindowClosed(); // Clean up resources when the window is closed
        //    base.OnClosed(e);
        //}






        //____________________________________________________________________________________________________________________________________________

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

        //not using in the new version.
        private async void finishButton_st1_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(">> " + lineNameCmb_st1.SelectedValue.ToString());

            var newTransaction = new TransactionLogBlockModel
            {
                //linename = lineNameCmb_st1.SelectedValue.ToString(),
                linename = lineName_st1,
                transportagent = lineMasterNameLbl_st1.Text,
                company = "නව ඇලන්වැලි තේ කම්හල",
                leaf_weight_officer = weightLeafOfficerTxt_st1.Text,
                //superviosr = supervisorCmb_st1.SelectedValue.ToString(),
                superviosr = supervisor_st1,
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
                box_weight = (int)Math.Ceiling(finalNBoxes_st1 * singleBoxWeight),

                final_green_leaf_count = finalAvailableNormalLeafWeight_st1,
                final_gold_leaf_count = finalAvailableGoldenLeafWeight_st1
            };

            System.Diagnostics.Debug.WriteLine(newTransaction);
            // Call the service to add the transaction
            bool _isok = await _consoleHandler.AddTransactionAsync(newTransaction);

            if (_isok)
            {
                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ ongoing cloud update ]::::::::::::");

                    //select the next press enter button as the target
                                          //clear all textboxes and labels(including table row, member & line master / name details)
                                          //keep these empty else both textboxes gets disabled by logic.
                    nSacksTxt_st1.Text = "";
                    nBoxesTxt_st1.Text = "";

                    wateredTxt_st1.Text = "";
                    rejectedTxt_st1.Text = "";
                    spoiledTxt_st1.Text = "";
                    maturedTxt_st1.Text = "";

                    normalLeafWeightTxt_st1.Text = "";
                    goldenLeafWeightTxt_st1.Text = "";
                    acceptedLeafWeightTxt_st1.Text = "";

                    barcodeTxt_st1.Text = "";
                    customerNameTxt_st1.Text = "";

                    lineMasterNameLbl_st1.Text = "";

                    //remove all table rows
                    MemberTurnTablePanel_st1.Children.Clear();
             }
             else
             {
                    MessageBox.Show(
                        "Upload failed, please try again",
                        "Upload Status",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
             }
            try
            {
                bool _isoks = await _consoleHandler.verifyTransactionsCloudCheck();
                if (_isoks)
                {
                    System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked done! ]:::::::::::::::");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked failed! ]::::::::::::");
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            ////an object like this is reusable.
            //var transactions = await _consoleHandler.GetTransactionData();

            //// Loop through each transaction and print details to the debug console
            //for (int i = 0; i < transactions.Count; i++)
            //{
            //    var transaction = transactions[i];
            //    System.Diagnostics.Debug.WriteLine($"Transaction {i + 1}:");
            //    System.Diagnostics.Debug.WriteLine($"Linename: {transaction.linename}");
            //    System.Diagnostics.Debug.WriteLine($"Transport Agent: {transaction.transportagent}");
            //    System.Diagnostics.Debug.WriteLine($"Company: {transaction.company}");
            //    System.Diagnostics.Debug.WriteLine($"Bag Count: {transaction.bag_count}");
            //    System.Diagnostics.Debug.WriteLine($"Total Leaf Weight: {transaction.total_leaf_weight}");
            //    System.Diagnostics.Debug.WriteLine("------------------------------");
            //}

            //System.Diagnostics.Debug.WriteLine("All transaction data printed successfully!");




            //this goes to the finish


        }

        //(not used in the new version)
        private  async void confirmAll_rounds_st2_Click(object sender, RoutedEventArgs e)
        {
            var Finaltransaction = new FinalTransactionBlockModel
            {
                //linename = lineNameCmb_st2.SelectedValue.ToString(),
                linename = lineName_st2,
                transportagent = lineMasterNameLbl_st2.Text,
                company = "නව ඇලන්වැලි තේ කම්හල",
                leaf_weight_officer = weightLeafOfficerTxt_st2.Text,
                //superviosr = supervisorCmb_st2.SelectedValue.ToString(),
                superviosr = supervisor_st2,
                barcode_details = barcodeTxt_st2.Text,
                name_with_initials = customerNameTxt_st2.Text,
                phone_number = "0712345678",
                date = DateTime.Now.ToString("yyyy-MM-dd"),  
                
                bag_count = finalNSacks_st2,

                maximum_nomal_leaf_weight = 90,
                total_leaf_weight = finalAcceptedLeafWeight_st2,
                actual_nomal_leaf_weight = finalNormalLeafWeight_st2,
                total_gold_leaf_weight = finalGoldenLeafWeight_st2,
                
                water = finalWateredWeight_st2,
                morapuwata = finalMaturedWeight_st2,
                thambimata = finalSpoiledWeight_st2,
                reject = finalRejectedWeight_st2,

                bag_weight = finalSackWeight_st2,

                final_green_leaf_count = finalAvailableNormalLeafWeight_st2,
                final_gold_leaf_count = finalAvailableGoldenLeafWeight_st2,
                real_value = finalWeightScalerWeight_st2 //TBDDD****************************************************************************
            };

/*      public int currentAcceptedSackWeightTxt_st2 = 0;
        //fill this during the api fetch w/ barcode
        public int currentAcceptedLeafWeight_st2 = 82;
        private double currentGoldenLeafWeight_st2 = 0;
        private double currentNormalLeafWeight_st2 = 0;

        public double currentTotalDeduction_st2 = 0;

        //used to count the number of rounds(for the table indexing and other purposes if necessary)
        private int _currentTurn_st2 = 1;
        //finalized values by each round to send to db/api
        private float finalWeightScalerWeight_st2 = 0; //this goes as the accepted value to api
        private int finalAcceptedLeafWeight_st2 = 0;
        private int finalGoldenLeafWeight_st2 = 0;
        private int finalNormalLeafWeight_st2 = 0;

        private int finalNBoxes_st2 = 0;
        private int finalNSacks_st2 = 0;

        private int finalWateredWeight_st2 = 0;
        private int finalMaturedWeight_st2 = 0;
        private int finalSpoiledWeight_st2 = 0;
        private int finalRejectedWeight_st2 = 0;

        private int finalAvailableGoldenLeafWeight_st2 = 0;
        private int finalAvailableNormalLeafWeight_st2 = 0;*/

        bool _isdone = await _consoleHandler.AddFinalTransactionAsync(Finaltransaction);

            if (_isdone)
            {
                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ *************** ]::::::::::::::::::::");
            }



            var transactionData = await _consoleHandler.GetTransactionData("001","");

            // Print the details in the debug console
            if (transactionData != null)
            {
                System.Diagnostics.Debug.WriteLine("Transaction Data:");
                System.Diagnostics.Debug.WriteLine($"> Line Name: {transactionData.linename}");
                System.Diagnostics.Debug.WriteLine($"> Transport Agent: {transactionData.transportagent}");
                System.Diagnostics.Debug.WriteLine($"> Company: {transactionData.company}");
                System.Diagnostics.Debug.WriteLine($"> Leaf Weight Officer: {transactionData.leaf_weight_officer}");
                System.Diagnostics.Debug.WriteLine($"> Supervisor: {transactionData.superviosr}");
                System.Diagnostics.Debug.WriteLine($"> Barcode Details: {transactionData.barcode_details}");
                System.Diagnostics.Debug.WriteLine($"> Name with Initials: {transactionData.name_with_initials}");
                System.Diagnostics.Debug.WriteLine($"> Phone Number: {transactionData.phone_number}");
                System.Diagnostics.Debug.WriteLine($"> Date: {transactionData.date}");
                System.Diagnostics.Debug.WriteLine($"> Bag Count: {transactionData.bag_count}");
                System.Diagnostics.Debug.WriteLine($"> Total Leaf Weight: {transactionData.total_leaf_weight}");
                // Add more fields as needed
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No transaction data found.");
            }


        }

        //used to startup the app with database verifications and closing & opening windows
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
            do
            {
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
                        failed = false;

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

                //verifying missing transactions and replacing them
                //try
                //{
                //    bool _isoks = await _consoleHandler.verifyTransactionsCloudCheck();
                //    if (_isoks)
                //    {
                //        System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked done! ]::::::::::::");
                //    }
                //    else
                //    {
                //        System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ cloud checked failed! ]::::::::::::");
                //    }
                //    failed = !_isoks;
                //    //await Task.Delay(1000);
                //}
                //catch (Exception ex)
                //{
                //    Dispatcher.Invoke(() =>
                //    {
                //        failed = true;
                //        statusLabel.Content = $"Member Database Error: {ex.Message}";
                //    });
                //}



            }
            while (failed);

            IntroFrame.Visibility = Visibility.Collapsed;
            LoginFrame.Visibility = Visibility.Visible;

            runtimeService.StartFileWatcher(); // Start watching the file
            runtimeService.StartTimer();

        }


    }
}

