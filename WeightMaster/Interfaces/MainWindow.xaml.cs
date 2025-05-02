using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using WeightMaster.Interfaces;
using WeightMaster.Interfaces.UserControls;
using WeightMaster.Models;
using WeightMaster.Services;
using System.Globalization;
using System.Windows.Documents;


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


        //for Enter and Esc navigation
        private int currentStep_st1 = 0;
        private const int maxStep_st1 = 7;
        private int currentStep_st2 = 0;
        private const int maxStep_st2 = 6;


        //public int nSacks_st1 = 0;
        //public int nBoxes_st1 = 0;
        private double singleBoxWeight = 3.5;


        //___________________st1____________________________________________________________________________________________|

        private string memberId_st1 = ""; //stores member id with all digits to send back to api.
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

        //used to send the station1 transactionData directly into station2 finalTransacitonData if wanted.
        private TransactionLogBlockModel currentMemberDetails_st2 = new TransactionLogBlockModel();

        //(IF IT'S A NEW ONE) Now holds a list of round objects for each additional round added for additional sack weight deduction
        private List<WeightRound_st2Model> addedRoundList = new List<WeightRound_st2Model>();
        //this can hold multiple values from multiple barcodeIDs
        private Dictionary<string, List<WeightRound_st2Model>> memberHashMap_st2 = new Dictionary<string, List<WeightRound_st2Model>>();

        private string lineName_st2 = "";
        private string supervisor_st2 = "";
        private double scalerRoundedWeight_st2 = 0;
        //public int currentAcceptedSackWeight_st2 = 0;
        //fill this during the api fetch w/ barcode
        public int currentAcceptedLeafWeight_st2 = 0;
        private double currentGoldenLeafWeight_st2 = 0;
        private double currentNormalLeafWeight_st2 = 0;

        public double currentTotalDeduction_st2 = 0;

        //used to count the number of rounds(for the table indexing and other purposes if necessary)
        private int _currentTurn_st2 = 1;
        //finalized values by each round to send to db/api
        private float finalWeightScalerWeight_st2 = 0; //this goes as the accepted value to api


        //private int finalAcceptedLeafWeight_st2 = 0;
        //private int finalGoldenLeafWeight_st2 = 0;
        //private int finalNormalLeafWeight_st2 = 0;

        //private int finalNSacks_st2 = 0;
        //private int finalSackWeight_st2 = 0;

        //private int finalWateredWeight_st2 = 0;
        //private int finalMaturedWeight_st2 = 0;
        //private int finalSpoiledWeight_st2 = 0;
        //private int finalRejectedWeight_st2 = 0;

        //private int finalAvailableGoldenLeafWeight_st2 = 0;
        //private int finalAvailableNormalLeafWeight_st2 = 0;


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
            //this console handler is used globally
            _consoleHandler = new ConsoleHandler();

            // Handles key presses globally(currently used to handle Enter key press for navigation)
            ShowCurrentStep();
            this.PreviewKeyDown += Window_KeyDown;

            //barcode related 
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += Timer_Tick;
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;

            StartupTheAppAsync();
            runtimeService.ExecuteRunExe();

        }

        //Enter & Esc navigation logic________________________________________________________________________________________

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    GoToNextStep();
                    break;
                case Key.Escape:
                    GoToPreviousStep();
                    break;
            }
        }

        private void GoToNextStep()
        {
            if (!ValidateCurrentStep()) return;

            if (Station1Frame.IsVisible)
            {
                //allow only textbox increments within goToNextStep method
                if (currentStep_st1 == maxStep_st1 /*|| currentStep_st1 == 1*/)
                {
                    //Handle final step completion
                    //MessageBox.Show("Process st1 completed!"); //moved to the click event to handle user navigation in case of a mouse click
                    //// reset the steps
                    //currentStep_st1 = -1;
                    //ShowCurrentStep();
                    //------------keep the return or otherwise step will incremented further by Enter event
                    //resetting step 5 to 0 and jumping from step 1 to 2 in Buttons is handled by click event to manage jumping in case of handling api failed or success outcomes
                    //ShowCurrentStep();
                    return;
                }

                if (currentStep_st1 >= maxStep_st1) return;

                currentStep_st1++; // this is reassigned at step1's and step5's click events
                ShowCurrentStep();
            }
            else if (Station2Frame.IsVisible)
            {
                if (currentStep_st2 == maxStep_st2)
                {
                    // Handle final step completion
                    //MessageBox.Show("Process st2 completed!");
                    // reset the steps
                    currentStep_st2 = 0;
                    ShowCurrentStep();
                    //return;
                }

                if (currentStep_st2 >= maxStep_st2) return;

                //allow only textbox increments
                //MessageBox.Show("incrementing");
                currentStep_st2++;
                ShowCurrentStep();
            }
        }

        private void GoToPreviousStep()
        {
            if (Station1Frame.IsVisible)
            {
                //avoid entering certain steps(steps with buttons)
                if (currentStep_st1 == 3)
                {
                    ShowCurrentStep();
                    return;
                }

                if (currentStep_st1 <= 1) return;

                currentStep_st1--;
                ShowCurrentStep();
            }
            else if (Station2Frame.IsVisible)
            {
                //avoid entering certain steps(steps with button clicks)
                if (currentStep_st2 == 4)
                {
                    currentStep_st2 = 2;
                    ShowCurrentStep();
                    return;
                }
                //avoid entering certain steps(steps with buttons)
                //if (currentStep_st2 == 2)
                //{
                //    ShowCurrentStep();
                //    return;
                //}
                //if (currentStep_st2 == 4)
                //{
                //    ShowCurrentStep();
                //    return;
                //}

                if (currentStep_st2 <= 1) return;

                currentStep_st2--;
                ShowCurrentStep();
            }
        }

        private void ShowCurrentStep()
        {
            if (Station1Frame.IsVisible)
            {
                // unfocus all steps
                weightScalerConfirmBtnBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                barcodeTxtBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                wateredTxtBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                confirmAddRowButtonBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                weightScalerBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                dataInputBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default

                //highlighting the input borders and weight scalers based on the step No.
                if (currentStep_st1 == 0 || currentStep_st1 == 1 || currentStep_st1 == 2)
                {
                    weightScalerBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // Default
                }
                else if (currentStep_st1 == 2 || currentStep_st1 == 3 || currentStep_st1 == 4 || currentStep_st1 == 5 || currentStep_st1 == 6)
                {
                    dataInputBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // Default
                }

                // Show current step
                switch (currentStep_st1)
                {
                    case 1:
                        weightScalerConfirmBtnBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111")); // focused
                        break;
                    case 2:
                        LoadStep1_st1();
                        weightScalerConfirmBtn_st1_Click(weightScalerConfirmBtn_st1, new RoutedEventArgs());
                        break;
                    case 3:
                        barcodeTxtBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // focused
                        barcodeTxt_st1.Focus();
                        barcodeTxt_st1.SelectAll();
                        LoadStep2_st1();
                        break;
                    case 4:
                        borderNSacks_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // focused
                        nSacksTxt_st1.Focus();
                        nSacksTxt_st1.SelectAll();
                        LoadStep3_st1();
                        break;
                    case 5:
                        wateredTxtBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // focused
                        wateredTxt_st1.Focus();
                        wateredTxt_st1.SelectAll();
                        LoadStep4_st1();
                        break;
                    case 6:
                        confirmAddRowButtonBorder_st1.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111")); // focused
                        break;
                    case 7:
                        LoadStep5_st1();
                        confirmAddRowButton_st1_Click(confirmAddRowButton_st1, new RoutedEventArgs());
                        break;
                }
            }
            if (Station2Frame.IsVisible)
            {
                // unfocus all steps
                wieghtScalerConfirmBtnBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                barcodeTxtBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                //borderNSacks_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                wateredTxtBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                confirmAddRowButtonBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                weightScalerBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default
                dataInputBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C4C4C4")); // Default

                //highlighting the input borders and weight scalers based on the step No.
                if (currentStep_st2 == 2)
                {
                    weightScalerBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // highlighted
                }
                else if (currentStep_st2 == 1 || currentStep_st2 == 3 || currentStep_st2 == 4)
                {
                    dataInputBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // highlighted
                }
                // Show current step
                switch (currentStep_st2)
                {
                    case 1:
                        barcodeTxtBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // focused
                        barcodeTxt_st2.Focus();
                        barcodeTxt_st2.SelectAll();
                        LoadStep2_st2();
                        break;
                    case 2:
                        wieghtScalerConfirmBtnBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111")); // focused
                        break;
                    case 3:
                        //MessageBox.Show("clearing focus in step2 st2");
                        Keyboard.ClearFocus();
                        LoadStep1_st2();
                        weightScalerConfirmBtn_st2_Click(weightScalerConfirmBtn_st2, new RoutedEventArgs());
                        break;
                    case 4:
                        wateredTxtBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // focused
                        wateredTxt_st2.Focus();
                        wateredTxt_st2.SelectAll();
                        LoadStep3_st2();
                        break;
                    case 5:
                        confirmAddRowButtonBorder_st2.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111")); // focused
                        break;
                    case 6:
                        LoadStep4_st2();
                        confirmAddRowButton_st2_Click(confirmAddRowButton_st2, new RoutedEventArgs());
                        //wateredTxt_st2.Select(0, 0);
                        Keyboard.ClearFocus();
                        this.Focus();
                        break;
                }
            }
        }

        private bool ValidateCurrentStep()
        {
            // Add validation logic for current step before proceeding
            if (Station1Frame.IsVisible)
            {
                switch (currentStep_st1)
                {
                    case 1:
                        return ValidateStep1_st1();
                    case 2:
                        return ValidateStep2_st1();
                    case 3:
                        return ValidateStep3_st1();
                    case 4:
                        return ValidateStep4_st1();
                    case 5:
                        return ValidateStep5_st1();
                    default:
                        return true;
                }
            }
            if (Station2Frame.IsVisible)
            {
                switch (currentStep_st2)
                {
                    case 1:
                        return ValidateStep1_st2();
                    case 2:
                        return ValidateStep2_st2();
                    case 3:
                        return ValidateStep3_st2();
                    case 4:
                        return ValidateStep4_st2();
                    default:
                        return true;
                }
            }
            else { return true; }
        }

        private void wieghtScalerConfirmBtn_GotFocus_st1(object sender, RoutedEventArgs e)
        {
            currentStep_st1 = 1;
            ShowCurrentStep();
        }
        private void barcodeTxt_GotFocus_st1(object sender, RoutedEventArgs e)
        {
            currentStep_st1 = 3;
            ShowCurrentStep();
        }
        private void nSacksTxt_GotFocus_st1(object sender, RoutedEventArgs e)
        {
            currentStep_st1 = 4;
            ShowCurrentStep();
        }
        private void wateredTxt_GotFocus_st1(object sender, RoutedEventArgs e)
        {
            currentStep_st1 = 5;
            ShowCurrentStep();
        }
        private void confirmAddRowButton_GotFocus_st1(object sender, RoutedEventArgs e)
        {
            currentStep_st1 = 6;
            ShowCurrentStep();
        }

        private void barcodeTxt_GotFocus_st2(object sender, RoutedEventArgs e)
        {
            currentStep_st2 = 1;
            ShowCurrentStep();
        }
        private void wieghtScalerConfirmBtn_GotFocus_st2(object sender, RoutedEventArgs e)
        {
            //commented 'cause i can handle this via button click event. provided that if there is no other reason to executing when focusing the button instead of clicking
            //currentStep_st2 = 3;
            //ShowCurrentStep();
        }
        //private void nSacksTxt_GotFocus_st2(object sender, RoutedEventArgs e)
        //{
        //    
        //    currentStep_st2 = 3;
        //    ShowCurrentStep();
        //}
        private void wateredTxt_GotFocus_st2(object sender, RoutedEventArgs e)
        {
            currentStep_st2 = 4;
            ShowCurrentStep();
        }
        private void confirmAddRowButton_GotFocus_st2(object sender, RoutedEventArgs e)
        {
            //commented 'cause i can handle this via button click event. provided that if there is no other reason to executing when focusing the button instead of clicking
            //currentStep_st2 = 6;
            //ShowCurrentStep();
        }


        #region Step-specific loading and validation Methods for st1

        private void LoadStep1_st1()
        {
            // Initialization code for Step 1
            Console.WriteLine("Loading Step 1");
        }

        private void LoadStep2_st1()
        {
            // Initialization code for Step 2
            Console.WriteLine("Loading Step 2");
        }

        private void LoadStep3_st1()
        {
            // Initialization code for Step 3
            Console.WriteLine("Loading Step 3");
        }

        private void LoadStep4_st1()
        {
            // Initialization code for Step 4
            Console.WriteLine("Loading Step 4");
        }

        private void LoadStep5_st1()
        {
            // Initialization code for Step 4
            Console.WriteLine("Loading Step 5");
        }

        private bool ValidateStep1_st1()
        {
            // Example validation
            return true; // Return false to block navigation
        }

        private bool ValidateStep2_st1()
        {
            //if (string.IsNullOrWhiteSpace(barcodeTxt_st1.Text))
            //{
            //    MessageBox.Show("Please enter text in Step 2!");
            //    return false;
            //}
            return true;
        }

        private bool ValidateStep3_st1()
        {
            //if (string.IsNullOrWhiteSpace(nSacksTxt_st1.Text))
            //{
            //    MessageBox.Show("Please enter text in Step 3!");
            //    return false;
            //}
            return true;
        }

        private bool ValidateStep4_st1()
        {
            //if (string.IsNullOrWhiteSpace(wateredTxt_st1.Text))
            //{
            //    MessageBox.Show("Please enter text in Step 4!");
            //    return false;
            //}
            return true;
        }

        private bool ValidateStep5_st1()
        {
            //if (string.IsNullOrWhiteSpace(wateredTxt_st1.Text))
            //{
            //    MessageBox.Show("Please enter text in Step 4!");
            //    return false;
            //}
            return true;
        }
        #endregion

        #region Step-specific loading and validation Methods for st2

        private void LoadStep1_st2()
        {
            // Initialization code for Step 1
            Console.WriteLine("Loading Step 1");
        }

        private void LoadStep2_st2()
        {
            // Initialization code for Step 2
            Console.WriteLine("Loading Step 2");
        }

        private void LoadStep3_st2()
        {
            // Initialization code for Step 4
            Console.WriteLine("Loading Step 4");
        }

        private void LoadStep4_st2()
        {
            // Initialization code for Step 4
            Console.WriteLine("Loading Step 5");
        }

        private bool ValidateStep1_st2()
        {
            // Example validation
            //if (string.IsNullOrWhiteSpace(barcodeTxt_st2.Text))
            //{
            //    MessageBox.Show("Please enter text in Step 1!");
            //    return false;
            //}
            return true; // Return false to block navigation
        }

        private bool ValidateStep2_st2()
        {

            return true;
        }

        private bool ValidateStep3_st2()
        {
            //if (string.IsNullOrWhiteSpace(wateredTxt_st2.Text))
            //{
            //    MessageBox.Show("Please enter text in Step 4!");
            //    return false;
            //}
            return true;
        }

        private bool ValidateStep4_st2()
        {
            //if (string.IsNullOrWhiteSpace(wateredTxt_st2.Text))
            //{
            //    MessageBox.Show("Please enter text in Step 4!");
            //    return false;
            //}
            return true;
        }
        #endregion

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


            if (!username.Equals("unknown"))
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

                //updating current Enter step
                currentStep_st1 = 1;
                currentStep_st2 = 1;
                ShowCurrentStep();
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

            //clear current steps for both stations
            currentStep_st1 = 1;
            currentStep_st2 = 1;

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
            barcodeTxt_st1.Text = "";

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

            customerNameTxt_st1.Text = "";
            lineMasterNameLbl_st1.Text = "";

            //lineNameCmb_st1.Items.Clear(); //..or
            //lineNameCmb_st1.SelectedIndex = -1;

            //keep these empty else both textboxes gets disabled by logic.
            nSacksTxt_st1.Text = "";
            nBoxesTxt_st1.Text = "";

            //st2
            barcodeTxt_st2.Text = "";

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

            customerNameTxt_st2.Text = "";
            lineMasterNameLbl_st2.Text = "";
            //st2 end

            if (customerWindow != null)
            {
                customerWindow.Close();
            }
            //hmm you need either to clear all textboxes or restart the app.
            System.Diagnostics.Debug.WriteLine("system check 1");
            //runtimeService.KillRunExe();
            //runtimeService.KillRunExe();
            //runtimeService.KillRunExe();
            System.Diagnostics.Debug.WriteLine("system check 2");
            StartupTheAppAsync();
        }


        //station1 frame_______________________________________________________________________________________________________________________

        private async void barcodeTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            memberId_st1 = barcodeTxt_st1.Text;
            //converts the member id to a 05-digit number by adding remaining zeros to the left.
            memberId_st1 = barcodeTxt_st1.Text.PadLeft(5, '0');
            string memberName = null;
            try
            {
                memberName = await _consoleHandler.GetMemberName(memberId_st1);
                customerNameTxt_st1.Text = memberName;
                if (memberName.Equals("No name with initials found"))
                {
                    customerNameTxt_st1.Text = "-";
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
                lineMasterNameLbl_st1.Text = result.LineMaster;

                try
                {
                    var data = await _consoleHandler.getDataByFilter(result.LineName.ToString());
                    System.Diagnostics.Debug.WriteLine(result.LineName.ToString());
                    //MessageBox.Show("here triggered");
                    if (data.Any())
                    {
                        // to assign into total values row
                        int rowNBoxes = 0;
                        int rowNSacks = 0;
                        int rowGoldenLeafWeight = 0;
                        int rowNormalLeafWeight = 0;
                        int rowTotalLeafWeight = 0;

                        //MessageBox.Show("data found");
                        foreach (var transaction in data)
                        {
                            Station1LineTableRow lr1 = new Station1LineTableRow(transaction.barcode_details.ToString(), transaction.bag_count.ToString(), transaction.box_count.ToString(), transaction.total_gold_leaf_weight.ToString(), transaction.actual_nomal_leaf_weight.ToString(), (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight).ToString());
                            LineTablePanel_st1.Children.Add(lr1);
                            //System.Diagnostics.Debug.WriteLine($"Transaction: Line Name: {transaction.linename}, Date: {transaction.date}, Box Count: {transaction.barcode_details}");
                            rowNBoxes += transaction.box_count;
                            rowNSacks += transaction.bag_count;
                            rowGoldenLeafWeight += transaction.total_gold_leaf_weight;
                            rowNormalLeafWeight += transaction.actual_nomal_leaf_weight;
                            rowTotalLeafWeight += (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight);
                        }
                        //assign total column values to the total values row
                        lineRowNBoxes_st1.Text = rowNBoxes.ToString();
                        lineRowNSacks_st1.Text = rowNSacks.ToString();
                        lineRowGoldLeafWeights_st1.Text = rowGoldenLeafWeight.ToString();
                        lineRowNormalLeafWeights_st1.Text = rowNormalLeafWeight.ToString();
                        lineRowTotalLeafWeights_st1.Text = rowTotalLeafWeight.ToString();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No transactions found for the specified line name and date.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error printing transactions by line name and date(for the confirm button): {ex.Message}");
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
            if (weightScalerConfirmBtnBorder_st1 == null)
                return;
            if (weightScalerConfirmBtn_st1 == null)
                return;
            if (!double.TryParse(weightScalerValTxt_st1.Text, out double scalerVal) || scalerVal < 0)
                scalerVal = 0;
            if (scalerVal > 0 || weightScalerStatus_st1.Text.Equals("සමබරයි"))
            {
                weightScalerConfirmBtn_st1.IsEnabled = true;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
            else
            {
                weightScalerConfirmBtn_st1.IsEnabled = false;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95D4A1"));
            }
        }

        private void weightScalerConfirmBtn_st1_Click(object sender, RoutedEventArgs e)
        {
            /*            bool passed = true;
                        if (passed)
                        {
                            // go to next step
                            currentStep_st1 = 3;
                            ShowCurrentStep(); //if this was called, enter will be pressed automatically
                        }
                        bool failed = false;
                        if (failed)
                        {
                            MessageBox.Show("Step 1 button click failed!");
                            currentStep_st1 = currentStep_st1 - 1;
                            //ShowCurrentStep(); //if this was executed, current step becomes zero hmm
                        }*/

            if (!weightScalerConfirmBtn_st1.IsEnabled)
            {
                //MessageBox.Show("the button is currently disabled");
                currentStep_st1 = currentStep_st1 - 1;
                ShowCurrentStep();
                return;
            }
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


                //enable and disable confirm buttons
                weightScalerConfirmBtn_st1.IsEnabled = false;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4")); // Gray out
                confirmAddRowButton_st1.IsEnabled = true;
                confirmAddRowButtonBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled)

                // go to next step
                currentStep_st1 = 3;
                ShowCurrentStep(); //if this was called, enter will be pressed automatically
            }
            else
            {
                MessageBox.Show(
                    "තරාදිය 0 නොවන සමබර විට කියවීම ගන්න",
                    "තරාදි කියවීම",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                //MessageBox.Show("Step 1 button click failed!"+currentStep_st1);
                currentStep_st1 = currentStep_st1 - 1;
                ShowCurrentStep(); //if this was executed, current step becomes highlighting the button hmm
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

            if (goldenLeafWeightTxt_st1.Text.Equals("0") && normalLeafWeightTxt_st1.Text.Equals("0")) { return; }

            //if golden leaf weight was changed
            if (goldenLeafWeightTxt_st1.IsKeyboardFocused)
            {
                //MessageBox.Show("goldleaf: event triggered.");
                normalLeafWeightTxt_st1.Text = ((acceptedLeafWeight - currentTotalDeduction_st1) - goldenLeafWeight).ToString();
                //update helper variables
                currentGoldenLeafWeight_st1 = goldenLeafWeight;
                //fix: stopped adding currentTotalDeduction to fix a calculation error
                currentNormalLeafWeight_st1 = (currentAcceptedLeafWeight_st1 /*+ currentTotalDeduction_st1*/) - goldenLeafWeight;
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
            if ((currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) - goldenLeafWeight >= 0)
            {
                normalLeafWeightTxt_st1.Text = ((currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1) - goldenLeafWeight).ToString();
                //update helper variables
                currentGoldenLeafWeight_st1 = goldenLeafWeight;
                currentNormalLeafWeight_st1 = (currentAcceptedLeafWeight_st1 /*+ currentTotalDeduction_st1*/) - goldenLeafWeight;
            }
            else //reset values if total deduction exceeds current golden leaf weight: to prevent errors like what if total deduction is more than the new totalSackWeight after limitation
            {
                //normalLeafWeightTxt_st1.Text = "0";
                //currentNormalLeafWeight_st1 = 0;
                //MessageBox.Show("triggered");
                //update helper variables
                //MessageBox.Show(""+currentAcceptedLeafWeight_st1+"-"+currentTotalDeduction_st1);
                //goldenLeafWeightTxt_st1.Text = (currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1).ToString();
                //currentGoldenLeafWeight_st1 = currentAcceptedLeafWeight_st1 - currentTotalDeduction_st1;

                //SHOULD i also zero the rounded value? i guess not?
                wateredTxt_st1.Text = "";
                spoiledTxt_st1.Text = "";
                maturedTxt_st1.Text = "";
                rejectedTxt_st1.Text = "";
                currentTotalDeduction_st1 = 0;

                goldenLeafWeightTxt_st1.Text = "0";
                currentGoldenLeafWeight_st1 = 0;

                if (nSacks != 0 && scalerRoundedWeight_st1 > sacksWeightLimit)
                {
                    //acceptedLeafWeightTxt_st1.Text = sacksWeightLimit.ToString();
                    //currentAcceptedLeafWeight_st1 = (int)sacksWeightLimit;
                    currentNormalLeafWeight_st1 = (int)sacksWeightLimit;
                    normalLeafWeightTxt_st1.Text = ((int)sacksWeightLimit).ToString();
                }
                else
                {
                    //acceptedLeafWeightTxt_st1.Text = scalerRoundedWeight_st1.ToString();
                    //currentAcceptedLeafWeight_st1 = (int)scalerRoundedWeight_st1;
                    currentNormalLeafWeight_st1 = (int)scalerRoundedWeight_st1;
                    normalLeafWeightTxt_st1.Text = ((int)scalerRoundedWeight_st1).ToString();
                }
                MessageBox.Show("done");
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
            if (scalerRoundedWeight_st1! > boxWeights)
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
                currentNormalLeafWeight_st1 = (currentAcceptedLeafWeight_st1 /*+ currentTotalDeduction_st1*/) - goldenLeafWeight;
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
                blueText.Text = currentTotalDeduction_st1.ToString();
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
            if (nBoxesTxt_st1.IsFocused)
            {
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
                        "සාමාජික දළු බර ඇතුලත් කරන්න",
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

            //clear the member id


            weightScalerConfirmBtn_st1.IsEnabled = true;
            confirmAddRowButton_st1.IsEnabled = false;
            weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled)
            confirmAddRowButtonBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4"));  // Gray out

        }


        private async void confirmAddRowButton_st1_Click(object sender, RoutedEventArgs e)
        {

/*            bool passed = true;
            if (passed)
            {
                //MessageBox.Show("Process st1 finished!");
                // reset the steps and focus the weight button
                currentStep_st1 = 1;
                ShowCurrentStep();
            }

            bool failed = false;
            if (failed)
            {
                //MessageBox.Show("Step 5 button click failed!");
                currentStep_st1 = currentStep_st1 - 1;
                ShowCurrentStep();
            }*/
            if (!confirmAddRowButton_st1.IsEnabled)
            {
                //MessageBox.Show("the button is currently disabled");
                currentStep_st1 = currentStep_st1 - 1;
                ShowCurrentStep();
                return;
            }
            //lineName_st1 = lineNameCmb_st1.SelectedValue.ToString();
            if (lineNameCmb_st1.SelectedValue != null)
            {
                lineName_st1 = lineNameCmb_st1.SelectedValue.ToString();
            }
            else
            {
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
                MessageBox.Show("අධීක්ෂණය හා ප්‍රවාහන මාර්හය ඇතුලත් කරන්න");
                loadingDataInputBorder_st1.Visibility = Visibility.Hidden;
                currentStep_st1 = currentStep_st1 - 1;
                ShowCurrentStep();
                return;
            }


            //clear and repopulate table rows(test) after successful update
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

                //cover the input grid with another grid to avoid misinputs and reconfirming the same request
                loadingDataInputBorder_st1.Visibility = Visibility.Visible;
                try
                {
                    var newTransaction = new TransactionLogBlockModel
                    {
                        linename = lineName_st1,
                        transportagent = lineMasterNameLbl_st1.Text,
                        company = "මොරවක්කොරළේ තේ කම්හල",
                        leaf_weight_officer = weightLeafOfficerTxt_st1.Text,
                        superviosr = supervisor_st1,
                        barcode_details = memberId_st1,
                        name_with_initials = customerNameTxt_st1.Text,
                        phone_number = "123-456-7890",
                        date = DateTime.Now.ToString("yyyy-MM-dd"),
                        box_count = nBoxes,
                        bag_count = nSacks,
                        real_value = weightScalerValue,
                        maximum_nomal_leaf_weight = (int)scalerRoundedWeight_st1,
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

                    // Call the service to add the transaction
                    _isSuccess = await _consoleHandler.AddTransactionAsync(newTransaction);

                    // enable and disable confirm buttons
                    weightScalerConfirmBtn_st1.IsEnabled = true;
                    confirmAddRowButton_st1.IsEnabled = false;
                    weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled)
                    confirmAddRowButtonBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4"));  // Gray out

                    //go back to the first step(focusing the barcode)
                    currentStep_st1 = 1;
                    ShowCurrentStep();
                }
                catch (Exception ex)
                {
                    // Handle exception here
                    System.Diagnostics.Debug.WriteLine($"Exception: {ex}");
                    loadingDataInputBorder_st1.Visibility = Visibility.Hidden;
                    currentStep_st1 = currentStep_st1 - 1;
                    ShowCurrentStep();
                }

                //go back to 1st step in Enter Press logic
                currentStep_st1 = 1;
                ShowCurrentStep();

                //MessageBox.Show("ready to populate"+_isSuccess.ToString());
                loadingDataInputBorder_st1.Visibility = Visibility.Hidden;
                if (_isSuccess)
                {
                    int totalWeight = (availableGoldenLeafWeight + availableNormalLeafWeight);
                    Station1TableRow station1TableRow1 = new Station1TableRow(_currentTurn_st1++.ToString(), nSacksTxt_st1.Text, nBoxesTxt_st1.Text, goldenLeafWeightTxt_st1.Text, normalLeafWeightTxt_st1.Text, /*totalWeight.ToString()*/acceptedLeafWeightTxt_st1.Text);
                    // When adding a new row dynamically(no need now)
                    //int insertIndex = MemberTurnTablePanel_st1.Children.Count - 1;
                    MemberTurnTablePanel_st1.Children.Add(station1TableRow1);
                    //MemberTurnTablePanel_st1.Children.Add(station1TableRow1);
                    //MessageBox.Show("data populated");


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

                    //barcodeTxt_st1.Text = ""; //this fucks up the text change event in barcode which leads to clearing the data in the turn table 

                    currentTotalDeduction_st1 = 0;
                    currentNormalLeafWeight_st1 = 0;
                    currentGoldenLeafWeight_st1 = 0;
                    currentAcceptedLeafWeight_st1 = 0;

                }
                else
                {
                    //int totalWeight = (availableGoldenLeafWeight + availableNormalLeafWeight);
                    //Station1TableRow station1TableRow1 = new Station1TableRow(_currentTurn_st1++.ToString(), nSacksTxt_st1.Text, nBoxesTxt_st1.Text, normalLeafWeightTxt_st1.Text, goldenLeafWeightTxt_st1.Text, /*totalWeight.ToString()*/acceptedLeafWeightTxt_st1.Text);
                    // When adding a new row dynamically(no need now)
                    //int insertIndex = MemberTurnTablePanel_st1.Children.Count - 1;
                    //MemberTurnTablePanel_st1.Children.Add(station1TableRow1);
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

                    //barcodeTxt_st1.Text = ""; //this fucks up the text change event in barcode which leads to clearing the data in the turn table 

                    currentTotalDeduction_st1 = 0;
                    currentNormalLeafWeight_st1 = 0;
                    currentGoldenLeafWeight_st1 = 0;
                    currentAcceptedLeafWeight_st1 = 0;

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

                // clear the linewise table to repopulate
                LineTablePanel_st1.Children.Clear();
                //updating the linewise table to show the confirmed transaction
                try
                {
                    var data = await _consoleHandler.getDataByFilter(lineNameCmb_st1.SelectedValue.ToString());
                    System.Diagnostics.Debug.WriteLine(lineNameCmb_st1.SelectedValue.ToString());
                    //MessageBox.Show("here triggered");
                    if (data.Any())
                    {
                        // to assign into total values row
                        int rowNBoxes = 0;
                        int rowNSacks = 0;
                        int rowGoldenLeafWeight = 0;
                        int rowNormalLeafWeight = 0;
                        int rowTotalLeafWeight = 0;

                        //MessageBox.Show("data found");
                        foreach (var transaction in data)
                        {
                            Station1LineTableRow lr1 = new Station1LineTableRow(transaction.barcode_details.ToString(), transaction.bag_count.ToString(), transaction.box_count.ToString(), transaction.total_gold_leaf_weight.ToString(), transaction.actual_nomal_leaf_weight.ToString(), (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight).ToString());
                            LineTablePanel_st1.Children.Add(lr1);
                            //System.Diagnostics.Debug.WriteLine($"Transaction: Line Name: {transaction.linename}, Date: {transaction.date}, Box Count: {transaction.barcode_details}");
                            rowNBoxes += transaction.box_count;
                            rowNSacks += transaction.bag_count;
                            rowGoldenLeafWeight += transaction.total_gold_leaf_weight;
                            rowNormalLeafWeight += transaction.actual_nomal_leaf_weight;
                            rowTotalLeafWeight += (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight);
                        }
                        //assign total column values to the total values row
                        lineRowNBoxes_st1.Text = rowNBoxes.ToString();
                        lineRowNSacks_st1.Text = rowNSacks.ToString();
                        lineRowGoldLeafWeights_st1.Text = rowGoldenLeafWeight.ToString();
                        lineRowNormalLeafWeights_st1.Text = rowNormalLeafWeight.ToString();
                        lineRowTotalLeafWeights_st1.Text = rowTotalLeafWeight.ToString();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No transactions found for the specified line name and date.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error printing transactions by line name and date(for the confirm button): {ex.Message}");
                }

            }
            else
            {
                MessageBox.Show("කරුණාකර සියලු තොරතුරු අතුලත් කරන්න");
                currentStep_st1 = currentStep_st1 - 1;
                ShowCurrentStep();
            }

            loadingDataInputBorder_st1.Visibility = Visibility.Hidden;
        }

        //station2 frame______________________________________________________________________________________________________________________________

        private async void barcodeTxt_st2_TextChanged(object sender, TextChangedEventArgs e)
        {
            string memberId = barcodeTxt_st2.Text.PadLeft(5, '0');;

            //clear member turn table for the next member(this is hidden currently)
            MemberTurnTablePanel_st2.Children.Clear();
            //clear all weight deduction rounds for the next id
            addedRoundList.Clear();

            String memberName = await _consoleHandler.GetMemberName(memberId);
            System.Diagnostics.Debug.WriteLine(barcodeTxt_st2 +" as "+memberId+ ": " + memberName);
            customerNameTxt_st2.Text = memberName;
            if (memberName.Equals("No name with initials found"))
            {
                customerNameTxt_st1.Text = "-";
            }


            //clear previous data if there are any
            currentMemberDetails_st2 = null;
            lineMasterNameLbl_st2.Text = "-";
            totalNSacksTxt_st2.Text = "";

            maturedTxt_st2.Text = "";
            wateredTxt_st2.Text = "";
            spoiledTxt_st2.Text = "";
            rejectedTxt_st2.Text = "";

            acceptedLeafWeightTxt_st2.Text = "";
            normalLeafWeightTxt_st2.Text = "";
            goldenLeafWeightTxt_st2.Text = "";

            currentAcceptedLeafWeight_st2 = 0;
            currentNormalLeafWeight_st2 = 0;
            currentGoldenLeafWeight_st2 = 0;

            currentTotalDeduction_st2 = 0;

            //get user data to populate textboxes and such 
            currentMemberDetails_st2 = await _consoleHandler.GetTransactionData(memberId, memberId);
            if (currentMemberDetails_st2 != null)
            {
                System.Diagnostics.Debug.WriteLine($"Line name: {currentMemberDetails_st2.linename}, Line Name: {currentMemberDetails_st2.Id}, Line Master: {currentMemberDetails_st2.transportagent}");
                //if (memberDetails == null)
                //    MessageBox.Show("member data is null");

                if (currentMemberDetails_st2 != null)
                {
                    //load and populate additional data like previous leaf data, box data like stuff
                    //tbd for transport route & agent
                    lineNameCmb_st2.SelectedItem = currentMemberDetails_st2.linename;
                    lineMasterNameLbl_st2.Text = currentMemberDetails_st2.transportagent;
                    //follow steps when inserting values to avoid collisions
                    totalNSacksTxt_st2.Text = currentMemberDetails_st2.bag_count.ToString();

                    //update the current values

                    maturedTxt_st2.Text = currentMemberDetails_st2.morapuwata.ToString();
                    wateredTxt_st2.Text = currentMemberDetails_st2.water.ToString();
                    spoiledTxt_st2.Text = currentMemberDetails_st2.thambimata.ToString();
                    rejectedTxt_st2.Text = currentMemberDetails_st2.reject.ToString();


                    //was i high when i wrote these?
                    //acceptedLeafWeightTxt_st2.Text = memberDetails.actual_nomal_leaf_weight.ToString();
                    //normalLeafWeightTxt_st2.Text = memberDetails.final_green_leaf_count.ToString();
                    //goldenLeafWeightTxt_st2.Text = memberDetails.final_gold_leaf_count.ToString();

                    //currentAcceptedLeafWeight_st2 = memberDetails.actual_nomal_leaf_weight;
                    //currentNormalLeafWeight_st2 = memberDetails.final_green_leaf_count;
                    //currentGoldenLeafWeight_st2 = memberDetails.final_gold_leaf_count;

                    //switched values(correct)
                    acceptedLeafWeightTxt_st2.Text = currentMemberDetails_st2.total_leaf_weight.ToString();
                    normalLeafWeightTxt_st2.Text = currentMemberDetails_st2.final_green_leaf_count.ToString();
                    goldenLeafWeightTxt_st2.Text = currentMemberDetails_st2.final_gold_leaf_count.ToString();

                    currentAcceptedLeafWeight_st2 = currentMemberDetails_st2.total_leaf_weight;
                    currentNormalLeafWeight_st2 = currentMemberDetails_st2.actual_nomal_leaf_weight;
                    currentGoldenLeafWeight_st2 = currentMemberDetails_st2.total_gold_leaf_weight;



                    currentTotalDeduction_st2 = currentMemberDetails_st2.morapuwata + currentMemberDetails_st2.water + currentMemberDetails_st2.reject + currentMemberDetails_st2.thambimata;

                    //MessageBox.Show(currentAcceptedLeafWeight_st2 + "= " + currentNormalLeafWeight_st2 + " + " + currentGoldenLeafWeight_st2 + "| total deduction: "+currentTotalDeduction_st2);
                }
                //.Text = "";
                //.Text = "";
                //public int currentAcceptedLeafWeight_st2 = 82;
                //private double currentGoldenLeafWeight_st2 = 0;
                //private double currentNormalLeafWeight_st2 = 0;

                //public double currentTotalDeduction_st2 = 0;

            }
            System.Diagnostics.Debug.WriteLine("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");


            //load the last round from the dictionary(hashmap) if it exists
            if (memberHashMap_st2.TryGetValue(memberId, out var weightRounds) && weightRounds.Count > 0)
            {
                var lastItem = weightRounds[weightRounds.Count - 1]; //weightRounds[^1];

                //load and populate additional data like previous leaf data, box data like stuff
                //follow steps when inserting values to avoid collisions
                //totalNSacksTxt_st2.Text = currentMemberDetails_st2.bag_count.ToString();

                maturedTxt_st2.Text = "";
                wateredTxt_st2.Text = "";
                spoiledTxt_st2.Text = "";
                rejectedTxt_st2.Text = "";

                acceptedLeafWeightTxt_st2.Text = "";
                normalLeafWeightTxt_st2.Text = "";
                goldenLeafWeightTxt_st2.Text = "";

                currentAcceptedLeafWeight_st2 = 0;
                currentNormalLeafWeight_st2 = 0;
                currentGoldenLeafWeight_st2 = 0;

                currentTotalDeduction_st2 = 0;


                //update the current values
                maturedTxt_st2.Text = lastItem.maturedWeight.ToString();
                wateredTxt_st2.Text = lastItem.wateredWeight.ToString();
                spoiledTxt_st2.Text = lastItem.spoiledWeight.ToString();
                rejectedTxt_st2.Text = lastItem.rejectedWeight.ToString();

                //switched values(correct)
                acceptedLeafWeightTxt_st2.Text = lastItem.currentAcceptedLeafWeight.ToString();
                normalLeafWeightTxt_st2.Text = lastItem.availableNormalLeafWeight.ToString();
                goldenLeafWeightTxt_st2.Text = lastItem.availableGoldenLeafWeight.ToString();

                currentAcceptedLeafWeight_st2 = lastItem.currentAcceptedLeafWeight;
                currentNormalLeafWeight_st2 = lastItem.currentAcceptedNormalLeafWeight;
                currentGoldenLeafWeight_st2 = lastItem.currentAcceptedGoldenWeight;


                //gives the wrong deductions placed at the begining.
                //currentTotalDeduction_st2 = currentMemberDetails_st2.morapuwata + currentMemberDetails_st2.water + currentMemberDetails_st2.reject + currentMemberDetails_st2.thambimata;
                currentTotalDeduction_st2 = lastItem.currentTotalDeduction;

                //populate the table also
                MemberTurnTablePanel_st2.Children.Clear();
                // Check if the key exists and retrieve the list
                if (memberHashMap_st2.TryGetValue(memberId, out List<WeightRound_st2Model> addedRoundList))
                {
                    // Loop through each item in the list
                    int rowIndex = 1;
                    foreach (WeightRound_st2Model round in addedRoundList)
                    {
                        Station2TableRow station2TableRow1 = new Station2TableRow(rowIndex++.ToString(), round.acceptedSackWeight.ToString(), (round.availableGoldenLeafWeight + round.availableNormalLeafWeight).ToString(), round.availableGoldenLeafWeight.ToString(), round.availableNormalLeafWeight.ToString());
                        MemberTurnTablePanel_st2.Children.Add(station2TableRow1);
                    }
                    rowIndex = 1;
                }
                else
                {
                    Console.WriteLine($"No entries found for key: {memberId}");
                }
            }
            else
            {
                // Clear UI or show a message
                //MessageBox.Show($"No data found for ID: {memberId}");
            }
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
            //#issue No2: 
            try
            {
                CustomerCompletionRowPanel.Children.Clear();
                var customerTransactions_st2 = await _consoleHandler.getDataByFilter(lineName);
                if (customerTransactions_st2 != null)
                {
                    //MessageBox.Show("result is null"); //result returns not null hmm
                    //for (int i = 0; i < customerTransactions_st2.Count; i++)
                    //{
                    //    var transaction = customerTransactions_st2[i];
                    //        MessageBox.Show($"Barcode: {transaction.barcode_details}");
                    //    System.Diagnostics.Debug.WriteLine($"Transaction {i + 1}:");
                    //    CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), "0", transaction.real_value.ToString(), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                    //    CustomerCompletionRowPanel.Children.Add(cctr4);
                    //}
                    foreach (var transaction in customerTransactions_st2)
                    {
                        CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), "0", transaction.real_value.ToString("F2", CultureInfo.CurrentCulture), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                        CustomerCompletionRowPanel.Children.Add(cctr4);
                        System.Diagnostics.Debug.WriteLine(transaction.barcode_details + " - " + transaction.linename);
                    }
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
            if (weightScalerConfirmBtn_st2 == null)
                return;
            if (!double.TryParse(weightScalerValTxt_st2.Text, out double scalerVal) || scalerVal < 0)
                scalerVal = 0;
            if (scalerVal > 0 || weightScalerStatus_st2.Text.Equals("සමබරයි"))
            {
                weightScalerConfirmBtn_st2.IsEnabled = true;
                wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
            else
            {
                weightScalerConfirmBtn_st2.IsEnabled = false;
                wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95D4A1"));
            }
        }


        private void weightScalerConfirmBtn_st2_Click(object sender, RoutedEventArgs e)
        {
            /*            bool isSuccess = true;
                        if (isSuccess)
                        {
                            currentStep_st2 = 4;
                            ShowCurrentStep();
                        }
                        //MessageBox.Show("weight getting done st2");
                        bool failed = false;
                        if (failed)
                        {
                            MessageBox.Show("Step 1 button click failed!");
                            currentStep_st2 = currentStep_st2 - 1;
                            ShowCurrentStep() ;
                        }*/

            //this click event should trigger after the data population via barcode id
            //so don't input the data to the textbox by this click before data weights population to prevent getting negative values and to follow the correct steps.

            if (!weightScalerConfirmBtn_st2.IsEnabled)
            {
                //MessageBox.Show("the button is currently disabled");
                currentStep_st2 = currentStep_st2 - 1;
                ShowCurrentStep();
                return;
            }

            if (acceptedLeafWeightTxt_st2.Text.Equals("") || acceptedLeafWeightTxt_st2.Text.Equals("0")) 
            {
                MessageBox.Show(
                    "පළමුව සාමාජික දළු බර ඇතුලත් කරන්න",
                    "තරාදි කියවීම",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            if (!double.TryParse(weightScalerValTxt_st2.Text, out double scalerWeight) || scalerWeight < 0)
                scalerWeight = 0;

            //int bagWeightLimit = 23;
            int ceilingValue = (int)Math.Ceiling(scalerWeight);
            // Check if the status equals "සමබරයි and it's not zero"
            if (weightScalerStatus_st2.Text == "සමබරයි" && scalerWeight != 0)
            {
                acceptedSackWeightTxt_st2.Text = ceilingValue.ToString();
                scalerRoundedWeight_st2 = ceilingValue;
                //currentAcceptedSackWeight_st2 = ceilingValue;
                // Get the largest integer less than or equal to the specified number
                //            int CeilingValue = (int)Math.Ceiling((double)weight);
                //            // Update the acceptedLeafWeightTxt_st1 TextBox/TextBlock
                //            acceptedSackWeightTxt_st2.Text = CeilingValue.ToString();
                //            currentAcceptedSackWeightTxt_st2 = CeilingValue;

                //to catch up with Enter key press event(in case of the manual click)
                ConsoleSound.PlayStable();
                confirmAddRowButton_st2.IsEnabled = true;
                weightScalerConfirmBtn_st2.IsEnabled = false;
                wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4"));  // Gray out
                confirmAddRowButtonBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled) 

                //go to next step
                currentStep_st2 = 4;
                ShowCurrentStep();
            }
            else
            {
                MessageBox.Show(
                    "තරාදිය 0 නොවන සමබර විට කියවීම ගන්න",
                    "තරාදි කියවීම",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                currentStep_st2 = currentStep_st2 - 1;
                ShowCurrentStep();
            }

        }

        //gold and normal leaf textbox logic
        private void GoldenAndNormalWeight_st2_TextChanged(object sender, TextChangedEventArgs e)
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

                if (goldenLeafWeightTxt_st2.Text.Equals("0") && normalLeafWeightTxt_st2.Text.Equals("0")) { return; }
                //MessageBox.Show("hmm");
                //if golden leaf weight was changed
                if (goldenLeafWeightTxt_st2.IsKeyboardFocused)
                {
                    //MessageBox.Show("focused w/ if");
                    //MessageBox.Show("goldleaf: event triggered.");
                    normalLeafWeightTxt_st2.Text = ((acceptedLeafWeight - currentTotalDeduction_st2) - goldenLeafWeight).ToString();
                    //update helper variables
                    currentGoldenLeafWeight_st2 = goldenLeafWeight;
                    currentNormalLeafWeight_st2 = (currentAcceptedLeafWeight_st2 /*- currentTotalDeduction_st2*/) - goldenLeafWeight;  //this was here: (currentAcceptedLeafWeight_st2 - currentTotalDeduction_st2)
                }

            //for testing purposes
            greenText2.Text = currentNormalLeafWeight_st2.ToString();
                goldText2.Text = currentGoldenLeafWeight_st2.ToString();

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


        //clear button st2
        private void clearBtn_st2_Clicked(object sender, RoutedEventArgs e)
        {
            string memberId = currentMemberDetails_st2.barcode_details;

            //clear any rounds if there are any
            //addedRoundList.Clear();

            //clear textboxes and helper variables
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
            //totalNSacksTxt_st2.Text = "";




            //load the last round
            if (memberHashMap_st2.TryGetValue(memberId, out var weightRounds) && weightRounds.Count > 0)
            {
                var lastItem =  weightRounds[weightRounds.Count - 1]; //weightRounds[^1];

                // Update UI controls with the last item's values
                //txtWeightScaler.Text = lastItem.weightScalerWeight.ToString();
                //txtSackWeight.Text = lastItem.acceptedSackWeight.ToString();
                //txtGoldenLeaf.Text = lastItem.currentAcceptedGoldenWeight.ToString("F2");
                //txtNormalLeaf.Text = lastItem.currentAcceptedNormalLeafWeight.ToString("F2");

                //load and populate additional data like previous leaf data, box data like stuff
                //follow steps when inserting values to avoid collisions
                //totalNSacksTxt_st2.Text = currentMemberDetails_st2.bag_count.ToString();

                //update the current values
                maturedTxt_st2.Text = lastItem.maturedWeight.ToString();
                wateredTxt_st2.Text = lastItem.wateredWeight.ToString();
                spoiledTxt_st2.Text = lastItem.spoiledWeight.ToString();
                rejectedTxt_st2.Text = lastItem.rejectedWeight.ToString();
                //adding this because now you can see the previous weight that you have captured and you can edit it.
                acceptedSackWeightTxt_st2.Text = lastItem.acceptedSackWeight.ToString();
                //switched values(correct)
                acceptedLeafWeightTxt_st2.Text = lastItem.currentAcceptedLeafWeight.ToString();
                normalLeafWeightTxt_st2.Text = lastItem.availableNormalLeafWeight.ToString();
                goldenLeafWeightTxt_st2.Text = lastItem.availableGoldenLeafWeight.ToString();

                currentAcceptedLeafWeight_st2 = lastItem.currentAcceptedLeafWeight;
                currentNormalLeafWeight_st2 = lastItem.currentAcceptedNormalLeafWeight;
                currentGoldenLeafWeight_st2 = lastItem.currentAcceptedGoldenWeight;



                currentTotalDeduction_st2 = currentMemberDetails_st2.morapuwata + currentMemberDetails_st2.water + currentMemberDetails_st2.reject + currentMemberDetails_st2.thambimata;
            }
            else
            {
                // Clear UI or show a message
                //MessageBox.Show($"No data found for ID: {memberId}");
            }

            //remove the last round so the new round can take it's place
            if (memberHashMap_st2.TryGetValue(memberId, out var list))
            {
                if (list.Count > 0)
                {
                    list.RemoveAt(list.Count - 1); // Remove last list item
                    Console.WriteLine($"Last item removed for key: {memberId}");
                }

                if (list.Count == 0)
                {
                    memberHashMap_st2.Remove(memberId); // Clean up value empty list along with the key id
                    Console.WriteLine($"Key '{memberId}' removed (list is now empty)");
                }
            }
            else
            {
                Console.WriteLine($"Key '{memberId}' does not exist.");
            }


            //repopulate the table with updated round details
            //show the newly added round in the table row based on member id/barcode text
            MemberTurnTablePanel_st2.Children.Clear();
            // Check if the key exists and retrieve the list
            if (memberHashMap_st2.TryGetValue(memberId, out List<WeightRound_st2Model> addedRoundList))
            {
                // Loop through each item in the list
                int rowIndex = 1;
                foreach (WeightRound_st2Model round in addedRoundList)
                {
                    Station2TableRow station2TableRow1 = new Station2TableRow(rowIndex++.ToString(), round.acceptedSackWeight.ToString(), (round.availableGoldenLeafWeight + round.availableNormalLeafWeight).ToString(), round.availableGoldenLeafWeight.ToString(), round.availableNormalLeafWeight.ToString());
                    MemberTurnTablePanel_st2.Children.Add(station2TableRow1);
                }
                rowIndex = 1;
            }
            else
            {
                Console.WriteLine($"No entries found for key: {memberId}");
            }

            //disable the upload button and enable the weightScaler button to get a new input for the next user
            confirmAddRowButton_st2.IsEnabled = false;
            weightScalerConfirmBtn_st2.IsEnabled = true;
            wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled)
            confirmAddRowButtonBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4"));  // Gray out
        }


        private void addRoundBtn_st2_Click(object sender, RoutedEventArgs e)
        {
            if (barcodeTxt_st2.Text.Equals(""))
            {
                MessageBox.Show("සාමාජික අංකය ඇතුලත් කරන්න");
                return;
            }

            if (acceptedSackWeightTxt_st2.Text.Equals("") || acceptedSackWeightTxt_st2.Text.Equals("0")) 
            {
                MessageBox.Show("පිලිගත් ගෝනි බර ඇතුලත් කරන්න");
                return;
            }

            if (lineNameCmb_st2.SelectedValue != null && !barcodeTxt_st2.Text.Equals("") && !acceptedSackWeightTxt_st2.Text.Equals("") && !acceptedLeafWeightTxt_st2.Text.Equals(""))
            {
                //int totalWeight = (Convert.ToInt32(goldenLeafWeightTxt_st2.Text) + Convert.ToInt32(normalLeafWeightTxt_st2.Text));

                string memberId = currentMemberDetails_st2.barcode_details;
                float.TryParse(weightScalerValTxt_st2.Text, out float weightScalerValue); //gives the sack weight in st2 please mind
                int.TryParse(acceptedLeafWeightTxt_st2.Text, out int acceptedLeafWeight);
                int.TryParse(acceptedSackWeightTxt_st2.Text, out int acceptedSackWeight);
                
                //int.TryParse(totalNSacksTxt_st2.Text, out int finalNSacks);

                int.TryParse(normalLeafWeightTxt_st2.Text, out int availableNormalLeafWeight);
                int.TryParse(goldenLeafWeightTxt_st2.Text, out int availableGoldenLeafWeight);

                int.TryParse(wateredTxt_st2.Text, out int wateredWeight);
                int.TryParse(maturedTxt_st2.Text, out int maturedWeight);
                int.TryParse(spoiledTxt_st2.Text, out int spoiledWeight);
                int.TryParse(rejectedTxt_st2.Text, out int rejectedWeight);

                //store the current round in the list for later usage
/*                addedRoundList.Add(
                    new WeightRound_st2Model(
                        weightScalerValue,
                        acceptedSackWeight,

                        currentGoldenLeafWeight_st2,
                        currentNormalLeafWeight_st2,
                        availableGoldenLeafWeight,
                        availableNormalLeafWeight,

                        wateredWeight,
                        maturedWeight,
                        spoiledWeight,
                        rejectedWeight
                    )
                 );*/

                //store the current round in the list to add to the dictionary
                var newRound = new WeightRound_st2Model(
                    weightScalerValue,
                    acceptedSackWeight,

                    currentGoldenLeafWeight_st2,
                    currentNormalLeafWeight_st2,
                    availableGoldenLeafWeight,
                    availableNormalLeafWeight,

                    wateredWeight,
                    maturedWeight,
                    spoiledWeight,
                    rejectedWeight
                );
                // add the [list item] OR create the [list with the item] into the dictionary
                if (memberHashMap_st2.TryGetValue(memberId, out var existingList))
                {
                    existingList.Add(newRound); // Add to the existing list
                }
                else
                {
                    // Create a new list and add the item
                    memberHashMap_st2.Add(memberId, new List<WeightRound_st2Model> { newRound });
                }

/*                //show the newly added round in the table row
                MemberTurnTablePanel_st2.Children.Clear();
                int rowIndex = 1;
                foreach (WeightRound_st2Model round in addedRoundList)
                {
                    //Console.WriteLine(round);
                    Station2TableRow station2TableRow1 = new Station2TableRow(rowIndex++.ToString(), round.acceptedSackWeight.ToString(), (round.availableGoldenLeafWeight+round.availableNormalLeafWeight).ToString(), round.availableGoldenLeafWeight.ToString(), round.availableNormalLeafWeight.ToString());
                    MemberTurnTablePanel_st2.Children.Add(station2TableRow1);
                }
                rowIndex = 1;*/

                //show the newly added round in the table row based on member id/barcode text
                MemberTurnTablePanel_st2.Children.Clear();
                // Check if the key exists and retrieve the list
                if (memberHashMap_st2.TryGetValue(memberId, out List<WeightRound_st2Model> addedRoundList))
                {
                    // Loop through each item in the list
                    int rowIndex = 1;
                    foreach (WeightRound_st2Model round in addedRoundList)
                    {
                        Station2TableRow station2TableRow1 = new Station2TableRow(rowIndex++.ToString(), round.acceptedSackWeight.ToString(), (round.availableGoldenLeafWeight + round.availableNormalLeafWeight).ToString(), round.availableGoldenLeafWeight.ToString(), round.availableNormalLeafWeight.ToString());
                        MemberTurnTablePanel_st2.Children.Add(station2TableRow1);
                    }
                    rowIndex = 1;
                }
                else
                {
                    Console.WriteLine($"No entries found for key: {memberId}");
                }



                //take the current hidden variable values before them shifting due to clearing
                int currGoldenLeafWeight = (int)currentGoldenLeafWeight_st2;
                int currNormalLeafWeight = (int)currentNormalLeafWeight_st2;
                int currAcceptedLeafWeight = currentAcceptedLeafWeight_st2;

                //prepare the values by clearing them first
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

                //make this zero and also deduct the accepted weight from this
                acceptedSackWeightTxt_st2.Text = "";
                //totalNSacksTxt_st2.Text = "";


                //populate textboxes and variables for the next new round by deducting the sack weight from the both sides of the equation
                //totalNSacksTxt_st2.Text = currentMemberDetails_st2.bag_count.ToString();

                //update the current values(please do the appropiate deductions before population)
                //MessageBox.Show("before: " + normalLeafWeightTxt_st2.Text);
                maturedTxt_st2.Text = maturedWeight.ToString();
                wateredTxt_st2.Text = wateredWeight.ToString();
                spoiledTxt_st2.Text = spoiledWeight.ToString();
                rejectedTxt_st2.Text = rejectedWeight.ToString();

                //deducted value for the next sack deduction
                acceptedLeafWeightTxt_st2.Text = (acceptedLeafWeight - acceptedSackWeight).ToString();
                //MessageBox.Show("mid: "+normalLeafWeightTxt_st2.Text);
                normalLeafWeightTxt_st2.Text = availableNormalLeafWeight.ToString();
                //MessageBox.Show("after: " + normalLeafWeightTxt_st2.Text);
                goldenLeafWeightTxt_st2.Text = availableGoldenLeafWeight.ToString();

                //deducted value for the next sack deduction
                currentAcceptedLeafWeight_st2 = currAcceptedLeafWeight - acceptedSackWeight;
                //FIX THISSSSSSSS: Yesss donee
                if (acceptedSackWeight <= currNormalLeafWeight) {
                    currentNormalLeafWeight_st2 = currNormalLeafWeight - acceptedSackWeight;
                    currentGoldenLeafWeight_st2 = currGoldenLeafWeight;
                }
                else if (acceptedSackWeight > currNormalLeafWeight) 
                {
                    currentNormalLeafWeight_st2 = 0;
                    currentGoldenLeafWeight_st2 = currGoldenLeafWeight - (acceptedSackWeight - currNormalLeafWeight);
                }
                //if normal weight doesn't exceeds total deduction(no need to update golden leaf weights)
/*                if (totalDeductions <= currentNormalLeafWeight_st2)
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
                }*/


                //re-enable weight scaler button and disable confirm button
                currentTotalDeduction_st2 = maturedWeight + wateredWeight + spoiledWeight + rejectedWeight;
                //MessageBox.Show("final: " + normalLeafWeightTxt_st2.Text);
                //MessageBox.Show("final: " + normalLeafWeightTxt_st2.Text);

                //enable the upload button and weightScaler button to get either edit the current sack weight or confirm existing rounds
                confirmAddRowButton_st2.IsEnabled = true;
                weightScalerConfirmBtn_st2.IsEnabled = true;
                wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4")); // green(enabled)
                confirmAddRowButtonBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4"));  // Gray out
            }
        }

        private async void confirmAddRowButton_st2_Click(object sender, RoutedEventArgs e)
        {
            //loadingDataInputBorder_st2
            /*            bool isSuccess = true;
                        if (isSuccess)
                        {
                            //MessageBox.Show("manually changing steps");
                            currentStep_st2 = 1;
                            ShowCurrentStep();
                        }
                        //MessageBox.Show("round confirmed st2");
                        bool hasfailed = false;
                        if (hasfailed)
                        {
                            //MessageBox.Show("Step 5 button click failed!");
                            currentStep_st2 = currentStep_st2 - 1;
                            ShowCurrentStep();
                        }*/

            if (!confirmAddRowButton_st2.IsEnabled)
            {
                //MessageBox.Show("the button is currently disabled");
                currentStep_st2 = currentStep_st2 - 1;
                ShowCurrentStep();
                return;
            }

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
                supervisor_st2 = "";
            }

            if (lineName_st2.Equals("") || supervisor_st2.Equals(""))
            {
                currentStep_st2 = currentStep_st2 - 1;
                MessageBox.Show("සාමාජික අංකය හා අධීක්ෂණ නිලධාරී ඇතුලත් කරන්න");
                currentStep_st2 = currentStep_st2 - 1;
                ShowCurrentStep();
                return;
            }


            if (lineNameCmb_st2.SelectedValue != null && !barcodeTxt_st2.Text.Equals("") && !acceptedSackWeightTxt_st2.Text.Equals("") && !acceptedLeafWeightTxt_st2.Text.Equals(""))
            {
                int totalWeight = (Convert.ToInt32(goldenLeafWeightTxt_st2.Text) + Convert.ToInt32(normalLeafWeightTxt_st2.Text));

                //Station1TableRow station1TableRow1 = new Station1TableRow(_currentTurn_st2++.ToString(), totalNSacksTxt_st2.Text, "", totalWeight.ToString(), goldenLeafWeightTxt_st2.Text, normalLeafWeightTxt_st2.Text);
                //MemberTurnTablePanel_st2.Children.Add(station1TableRow1);

                string memberId = currentMemberDetails_st2.barcode_details;

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



                loadingDataInputBorder_st2.Visibility = Visibility.Visible;
                bool _isdone = false;

                //store the last current round in the list for the calculations with previous rounds(old)
                //addedRoundList.Add(
                //    new WeightRound_st2Model
                //    (
                //        finalWeightScalerValue,
                //        finalAcceptedSackWeight,

                //        currentGoldenLeafWeight_st2,
                //        currentNormalLeafWeight_st2,
                //        finalAvailableGoldenLeafWeight,
                //        finalAvailableNormalLeafWeight,

                //        finalWateredWeight,
                //        finalMaturedWeight,
                //        finalSpoiledWeight,
                //        finalRejectedWeight
                //    )
                // );

                //(new)
                //add the last final round to the list
                WeightRound_st2Model finalRound = new WeightRound_st2Model
                    (
                        finalWeightScalerValue,
                        finalAcceptedSackWeight,

                        currentGoldenLeafWeight_st2,
                        currentNormalLeafWeight_st2,
                        finalAvailableGoldenLeafWeight,
                        finalAvailableNormalLeafWeight,

                        finalWateredWeight,
                        finalMaturedWeight,
                        finalSpoiledWeight,
                        finalRejectedWeight
                    );

                // Check if the key exists
                if (memberHashMap_st2.ContainsKey(memberId))
                {
                    // Key exists: Add the item to the existing list
                    memberHashMap_st2[memberId].Add(finalRound);
                }
                else
                {
                    // Key does NOT exist: Create a new list, add the item, and add to the dictionary
                    memberHashMap_st2.Add(memberId, new List<WeightRound_st2Model> { finalRound });
                }




                // Sum up each property to upload with the finalTransaciton
                //float totalWeightSaclaerValue = addedRoundList.Sum(r => r.weightScalerWeight);
                //int totalAcceptedSackWeight = addedRoundList.Sum(r => r.acceptedSackWeight);

                // Sum up each property to upload with the finalTransaciton
                float totalWeightSaclaerValue = 0;
                int totalAcceptedSackWeight = 0;

                if (memberHashMap_st2.TryGetValue(memberId, out List<WeightRound_st2Model> weightRounds))
                {
                    totalWeightSaclaerValue = weightRounds.Sum(round => round.weightScalerWeight);
                    totalAcceptedSackWeight = weightRounds.Sum(round => round.acceptedSackWeight);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Key '{memberId}' not found.");
                }

                try
                {
                    var Finaltransaction = new FinalTransactionBlockModel
                    {
                        //linename = lineNameCmb_st2.SelectedValue.ToString(),
                        Id = 0,
                        linename = lineName_st2,
                        transportagent = lineMasterNameLbl_st2.Text,
                        company = "මොරවක්කෝරළේ තේ කම්හල",
                        leaf_weight_officer = weightLeafOfficerTxt_st2.Text,
                        //superviosr = supervisorCmb_st2.SelectedValue.ToString(),
                        //superviosr = supervisor_st2,
                        superviosr = currentMemberDetails_st2.superviosr,
                        barcode_details = currentMemberDetails_st2.barcode_details,
                        name_with_initials = customerNameTxt_st2.Text,
                        phone_number = "0712345678",
                        date = DateTime.Now.ToString("yyyy-MM-dd"),

                        bag_count = finalNSacks,

                        maximum_nomal_leaf_weight = currentMemberDetails_st2.maximum_nomal_leaf_weight,
                        total_leaf_weight = currentMemberDetails_st2.total_leaf_weight,
                        actual_nomal_leaf_weight = currentMemberDetails_st2.actual_nomal_leaf_weight,
                        total_gold_leaf_weight = currentMemberDetails_st2.total_gold_leaf_weight,

                        water = finalWateredWeight,
                        morapuwata = finalMaturedWeight,
                        thambimata = finalSpoiledWeight,
                        reject = finalRejectedWeight,

                        bag_weight = totalAcceptedSackWeight,

                        final_green_leaf_count = finalAvailableNormalLeafWeight,
                        final_gold_leaf_count = finalAvailableGoldenLeafWeight,
                        real_value = totalWeightSaclaerValue
                    };

                    //removes the key value pair upon a successful insertion.
                    memberHashMap_st2.Remove(memberId);
                    /*var Finaltransaction = new FinalTransactionBlockModel
                    {
                        //linename = lineNameCmb_st2.SelectedValue.ToString(),
                        Id = 0,
                        linename = lineName_st2,
                        transportagent = lineMasterNameLbl_st2.Text,
                        company = "මොරවක්කෝරළේ තේ කම්හල",
                        leaf_weight_officer = weightLeafOfficerTxt_st2.Text,
                        //superviosr = supervisorCmb_st2.SelectedValue.ToString(),
                        superviosr = supervisor_st2,
                        barcode_details = barcodeTxt_st2.Text,
                        name_with_initials = customerNameTxt_st2.Text,
                        phone_number = "0712345678",
                        date = DateTime.Now.ToString("yyyy-MM-dd"),

                        bag_count = finalNSacks,

                        maximum_nomal_leaf_weight = 23,
                        total_leaf_weight = finalAcceptedLeafWeight,
                        actual_nomal_leaf_weight = (int)currentNormalLeafWeight_st2,
                        total_gold_leaf_weight = (int)currentGoldenLeafWeight_st2,

                        water = finalWateredWeight,
                        morapuwata = finalMaturedWeight,
                        thambimata = finalSpoiledWeight,
                        reject = finalRejectedWeight,

                        bag_weight = finalAcceptedSackWeight,

                        final_green_leaf_count = finalAvailableNormalLeafWeight,
                        final_gold_leaf_count = finalAvailableGoldenLeafWeight,
                        real_value = finalWeightScalerValue //TBDDD****************************************************************************
                    };*/

                    _isdone = await _consoleHandler.AddFinalTransactionAsync(Finaltransaction, currentMemberDetails_st2.barcode_details);

                    weightScalerConfirmBtn_st2.IsEnabled = true;
                    confirmAddRowButton_st2.IsEnabled = false;
                    wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled)
                    confirmAddRowButtonBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4"));  // Gray out
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("Upload Failed: " + ex.Message);
                    loadingDataInputBorder_st2.Visibility = Visibility.Hidden;
                    currentStep_st2 = currentStep_st2 - 1;
                    ShowCurrentStep();
                }

                currentStep_st2 = 1;
                ShowCurrentStep();

                loadingDataInputBorder_st2.Visibility = Visibility.Hidden;

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

                    barcodeTxt_st2.Text = "";
                }
                else //hrrngh what is this
                {
                    System.Diagnostics.Debug.WriteLine("upload failed... please try again");
                    //MessageBox.Show("upload failed... please try again,");
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

                    barcodeTxt_st2.Text = "";
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
                    var customerTransactions_st2 = await _consoleHandler.getDataByFilter(lineName);
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
                currentStep_st2 = currentStep_st2 - 1;
                ShowCurrentStep();
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
                //MessageBox.Show(
                //    "Upload failed, please try again",
                //    "Upload Status",
                //    MessageBoxButton.OK,
                //    MessageBoxImage.Information
                //);
                System.Diagnostics.Debug.WriteLine("upload failed... please try again");
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
/*        private async void confirmAll_rounds_st2_Click(object sender, RoutedEventArgs e)
        {
*//*            var Finaltransaction = new FinalTransactionBlockModel
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
            };*/

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
                    private int finalAvailableNormalLeafWeight_st2 = 0;

                    bool _isdone = await _consoleHandler.AddFinalTransactionAsync(Finaltransaction);
            

            if (_isdone)
            {
                System.Diagnostics.Debug.WriteLine(":::::::::::::::::::::[ *************** ]::::::::::::::::::::");
            }
            *//*


            var transactionData = await _consoleHandler.GetTransactionData("001", "");

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


        }*/

        //used to startup the app with database verifications and closing & opening windows

        private async void StartupTheAppAsync()
        {
            // Closing existing pages
            Station1Frame.Visibility = Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;
            SettingsFrame.Visibility = Visibility.Collapsed;
            StationMainFrame.Visibility = Visibility.Collapsed;
            IntroFrame.Visibility = Visibility.Visible;

            bool internetAvailable = false;
            bool failed = false;

            // Check internet connection
            try
            {
                Dispatcher.Invoke(() =>
                {
                    statusLabel.Content = "Checking Internet Connection...";
                });

                internetAvailable = await Task.Run(() =>
                {
                    try
                    {
                        using (var client = new System.Net.WebClient())
                        using (client.OpenRead("https://www.google.com"))
                            return true;
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (!internetAvailable)
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "No internet connection. Startup aborted.";
                    });

                    IntroFrame.Visibility = Visibility.Collapsed;
                    LoginFrame.Visibility = Visibility.Visible;

                    runtimeService.StartFileWatcher();
                    runtimeService.StartTimer();

                    return;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    statusLabel.Content = $"Error checking internet: {ex.Message}";
                });
                return;
            }

            // Repeat until no exception occurs
            do
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "Verifying Database Status(1)...";
                    });

                    await _consoleHandler.VerifyUserDb(); // Uncomment when needed

                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(1)";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"User Database Error: {ex.Message}";
                    });
                }

                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "Verifying Database Status(2)...";
                    });

                    await _consoleHandler.VerifyLineMasterDb(); // Uncomment when needed

                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(2)";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"LineMaster Database Error: {ex.Message}";
                    });
                }

                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "Verifying Database Status(3)...";
                    });

                    await _consoleHandler.verifyMemberDb(); // Uncomment when needed

                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(3)";
                        failed = false;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"Member Database Error: {ex.Message}";
                    });
                }

                // Transaction verification is commented out, include it when needed
                // try
                // {
                //     bool _isoks = await _consoleHandler.verifyTransactionsCloudCheck();
                //     failed = !_isoks;
                // }
                // catch (Exception ex)
                // {
                //     Dispatcher.Invoke(() =>
                //     {
                //         failed = true;
                //         statusLabel.Content = $"Transaction Error: {ex.Message}";
                //     });
                // }

            } while (failed);

            IntroFrame.Visibility = Visibility.Collapsed;
            LoginFrame.Visibility = Visibility.Visible;

            runtimeService.StartFileWatcher();
            runtimeService.StartTimer();
        }



        //_______Reports_______________________________________________________________________

        private async void printLineReportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (lineNameCmb_st2.SelectedItem == null)
            {
                MessageBox.Show("ප්‍රවා හන මා ර්ගය ඇතුලත් කරන්න");
                return;
            }

            try
            {
                //MessageBox.Show(lineNameCmb_st2.SelectedItem.ToString());
                //fetch line wise data to pass down to report drawing
                List<FinalTransactionBlockModel> lineReportData = await _consoleHandler.print_sta2(lineNameCmb_st2.SelectedItem.ToString());
                //MessageBox.Show("db call passed here");
                //testing the data
                //if (lineReportData != null)
                //{
                //MessageBox.Show("response is not null");
                //}
                foreach (var transaction in lineReportData)
                {
                    System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
                }


                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    DrawingVisual visual = new DrawingVisual();
                    using (DrawingContext dc = visual.RenderOpen())
                    {
                        DrawLineReportPage(dc, lineReportData, lineNameCmb_st2.SelectedItem.ToString());
                    }
                    printDialog.PrintVisual(visual, "Print Document");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Report data fetching error: " + ex.Message);
            }
        }


        private void DrawLineReportPage(DrawingContext dc, List<FinalTransactionBlockModel> lineReportData, string lineName)
        {
            foreach (var transaction in lineReportData)
            {
                System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
            }

            Typeface typeface = new Typeface("Arial");
            double fontSize = 10;
            Brush brush = Brushes.Black;
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double yPos = 50;
            double pageWidth = 816;

            // Headers
            string[] mainHeaders = {
        "සීමාසහිත මොරවක්කොරළේ තේ නිපදවනන්ගේ සමුපකාර සමිතිය",
        "සමූපකාර තේ කම්හල",
        lineName
    };
            double[] headerSizes = { 14, 14, 12 };

            for (int i = 0; i < mainHeaders.Length; i++)
            {
                FormattedText headerText = new FormattedText(
                    mainHeaders[i],
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    headerSizes[i],
                    brush,
                    pixelsPerDip
                );

                double centerX = (pageWidth - headerText.WidthIncludingTrailingWhitespace) / 2;
                dc.DrawText(headerText, new Point(centerX, yPos));
                yPos += headerText.Height + 8;
            }

            yPos += 30;

            // Report Issue Index
            string issueIndex = "IDX-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            dc.DrawText(
                new FormattedText("Report Issue Index: " + issueIndex, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, fontSize, brush, pixelsPerDip),
                new Point(50, yPos));
            yPos += 20;

            // Date Issued
            dc.DrawText(
                new FormattedText("Date Issued: " + DateTime.Now.ToString("yyyy.MM.dd"), CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip),
                new Point(50, yPos));
            yPos += 40;

            // Table headers
            string[] headers = {
        "අංකය", "සාමාජික අංකය", "ගෝනි(n)", "පෙට්ටි(n)",
        "මුළු බර", "වතුරට", "මෝරපුවට", "තැමිණීමට",
        "ප්‍රතික්ෂේපිත", "ගෝනි බර", "දළු බර"
    };
            double[] headerPositions = { 50, 110, 200, 260, 320, 380, 450, 510, 570, 640, 710 };

            // Draw black background for header
            dc.DrawRectangle(Brushes.Black, null, new Rect(40, yPos - 5, pageWidth - 80, 25));

            for (int i = 0; i < headers.Length; i++)
            {
                dc.DrawText(
                    new FormattedText(headers[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, Brushes.White, pixelsPerDip),
                    new Point(headerPositions[i], yPos));
            }

            yPos += 25;

            // Totals
            int totalBagCount = 0, totalBoxCount = 0, totalLeafWeight = 0;
            int totalWater = 0, totalMorapuwata = 0, totalThambimata = 0, totalReject = 0, totalBagWeight = 0, totalDalu = 0;

            List<string[]> data = new List<string[]>();
            foreach (var transaction in lineReportData)
            {
                int boxCount = transaction.bag_count > 0 ? 0 : 0;
                int dalu = transaction.total_leaf_weight - (transaction.water + transaction.morapuwata
                             + transaction.thambimata + transaction.reject + transaction.bag_weight);

                data.Add(new string[]
                {
                transaction.Id.ToString(),
                transaction.barcode_details ?? "",
                transaction.bag_count.ToString(),
                boxCount.ToString(),
                transaction.total_leaf_weight.ToString(),
                transaction.water.ToString(),
                transaction.morapuwata.ToString(),
                transaction.thambimata.ToString(),
                transaction.reject.ToString(),
                transaction.bag_weight.ToString(),
                dalu.ToString()
                });

                totalBagCount += transaction.bag_count;
                totalBoxCount += boxCount;
                totalLeafWeight += transaction.total_leaf_weight;
                totalWater += transaction.water;
                totalMorapuwata += transaction.morapuwata;
                totalThambimata += transaction.thambimata;
                totalReject += transaction.reject;
                totalBagWeight += transaction.bag_weight;
                totalDalu += dalu;
            }

            // Draw rows
            bool isAlternate = false;
            foreach (string[] row in data)
            {
                if (isAlternate)
                {
                    dc.DrawRectangle(Brushes.LightGray, null, new Rect(40, yPos - 2, pageWidth - 80, 20));
                }
                for (int i = 0; i < row.Length; i++)
                {
                    dc.DrawText(
                        new FormattedText(row[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                            typeface, fontSize, brush, pixelsPerDip),
                        new Point(headerPositions[i], yPos));
                }
                yPos += 20;
                isAlternate = !isAlternate;
            }

            // Totals row background
            dc.DrawRectangle(Brushes.DarkGray, null, new Rect(40, yPos - 2, pageWidth - 80, 20));

            string[] totalsRow = {
        "Total", "", totalBagCount.ToString(), totalBoxCount.ToString(), totalLeafWeight.ToString(),
        totalWater.ToString(), totalMorapuwata.ToString(), totalThambimata.ToString(), totalReject.ToString(),
        totalBagWeight.ToString(), totalDalu.ToString()
    };

            for (int i = 0; i < totalsRow.Length; i++)
            {
                dc.DrawText(
                    new FormattedText(totalsRow[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, brush, pixelsPerDip),
                    new Point(headerPositions[i], yPos));
            }

            yPos += 40;

            // Signature areas - Modified section
            double signY = yPos + 60;
            double signatureLineLength = 300; // Length of each signature line

            // Supervisor Signature (left side)
            dc.DrawLine(new Pen(brush, 1), new Point(50, signY), new Point(50 + signatureLineLength, signY));
            dc.DrawText(
                new FormattedText("Authorised by (Supervisor)", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip),
                new Point(50, signY + 5));

            // Leaf Weighting Officer Signature (right side)
            double rightSignatureStartX = pageWidth - 50 - signatureLineLength; // 50px margin from right
            dc.DrawLine(new Pen(brush, 1), new Point(rightSignatureStartX, signY), new Point(rightSignatureStartX + signatureLineLength, signY));
            dc.DrawText(
                new FormattedText("Authorised by (Leaf Weighting Officer)", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip),
                new Point(rightSignatureStartX, signY + 5));

        }



        //_________________daily report___________________________________

        private async void printDailyReportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (lineNameCmb_st2.SelectedItem == null)
            {
                MessageBox.Show("ප්‍රවා හන මා ර්ගය ඇතුලත් කරන්න");
                return;
            }

            try
            {
                //MessageBox.Show(lineNameCmb_st2.SelectedItem.ToString());
                //fetch line wise data to pass down to report drawing
                List<FinalTransactionBlockModel> lineReportData = await _consoleHandler.print_sta2(lineNameCmb_st2.SelectedItem.ToString());
                //MessageBox.Show("db call passed here");
                //testing the data
                //if (lineReportData != null)
                //{
                //MessageBox.Show("response is not null");
                //}
                foreach (var transaction in lineReportData)
                {
                    System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
                }


                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    DrawingVisual visual = new DrawingVisual();
                    using (DrawingContext dc = visual.RenderOpen())
                    {
                        DrawDailyReportPage(dc, lineReportData);
                    }
                    printDialog.PrintVisual(visual, "Print Document");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Report data fetching error: " + ex.Message);
            }
        }

        private void DrawDailyReportPage(DrawingContext dc, List<FinalTransactionBlockModel> lineReportData)
        {
            foreach (var transaction in lineReportData)
            {
                System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
            }

            Typeface typeface = new Typeface("Arial");
            double fontSize = 10;
            Brush brush = Brushes.Black;
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double yPos = 50;
            double pageWidth = 816;

            // Headers
            string[] mainHeaders = {
        "සීමාසහිත මොරවක්කොරළේ තේ නිපදවනන්ගේ සමුපකාර සමිතිය",
        "සමූපකාර තේ කම්හල",
    };
            double[] headerSizes = { 14, 14 };

            for (int i = 0; i < mainHeaders.Length; i++)
            {
                FormattedText headerText = new FormattedText(
                    mainHeaders[i],
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    headerSizes[i],
                    brush,
                    pixelsPerDip
                );

                double centerX = (pageWidth - headerText.WidthIncludingTrailingWhitespace) / 2;
                dc.DrawText(headerText, new Point(centerX, yPos));
                yPos += headerText.Height + 8;
            }

            yPos += 30;

            // Report Issue Index
            string issueIndex = "IDX-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            dc.DrawText(
                new FormattedText("Report Issue Index: " + issueIndex, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, fontSize, brush, pixelsPerDip),
                new Point(50, yPos));
            yPos += 20;

            // Date Issued
            dc.DrawText(
                new FormattedText("Date Issued: " + DateTime.Now.ToString("yyyy.MM.dd"), CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip),
                new Point(50, yPos));
            yPos += 40;

            // Table headers
            string[] headers = {
        "අංකය", "සාමාජික අංකය", "ගෝනි(n)", "පෙට්ටි(n)",
        "මුළු බර", "වතුරට", "මෝරපුවට", "තැමිණීමට",
        "ප්‍රතික්ෂේපිත", "ගෝනි බර", "දළු බර"
    };
            double[] headerPositions = { 50, 110, 200, 260, 320, 380, 450, 510, 570, 640, 710 };

            // Draw black background for header
            dc.DrawRectangle(Brushes.Black, null, new Rect(40, yPos - 5, pageWidth - 80, 25));

            for (int i = 0; i < headers.Length; i++)
            {
                dc.DrawText(
                    new FormattedText(headers[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, Brushes.White, pixelsPerDip),
                    new Point(headerPositions[i], yPos));
            }

            yPos += 25;

            // Totals
            int totalBagCount = 0, totalBoxCount = 0, totalLeafWeight = 0;
            int totalWater = 0, totalMorapuwata = 0, totalThambimata = 0, totalReject = 0, totalBagWeight = 0, totalDalu = 0;

            List<string[]> data = new List<string[]>();
            foreach (var transaction in lineReportData)
            {
                int boxCount = transaction.bag_count > 0 ? 0 : 0;
                int dalu = transaction.total_leaf_weight - (transaction.water + transaction.morapuwata
                             + transaction.thambimata + transaction.reject + transaction.bag_weight);

                data.Add(new string[]
                {
                transaction.Id.ToString(),
                transaction.barcode_details ?? "",
                transaction.bag_count.ToString(),
                boxCount.ToString(),
                transaction.total_leaf_weight.ToString(),
                transaction.water.ToString(),
                transaction.morapuwata.ToString(),
                transaction.thambimata.ToString(),
                transaction.reject.ToString(),
                transaction.bag_weight.ToString(),
                dalu.ToString()
                });

                totalBagCount += transaction.bag_count;
                totalBoxCount += boxCount;
                totalLeafWeight += transaction.total_leaf_weight;
                totalWater += transaction.water;
                totalMorapuwata += transaction.morapuwata;
                totalThambimata += transaction.thambimata;
                totalReject += transaction.reject;
                totalBagWeight += transaction.bag_weight;
                totalDalu += dalu;
            }

            // Draw rows
            bool isAlternate = false;
            foreach (string[] row in data)
            {
                if (isAlternate)
                {
                    dc.DrawRectangle(Brushes.LightGray, null, new Rect(40, yPos - 2, pageWidth - 80, 20));
                }
                for (int i = 0; i < row.Length; i++)
                {
                    dc.DrawText(
                        new FormattedText(row[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                            typeface, fontSize, brush, pixelsPerDip),
                        new Point(headerPositions[i], yPos));
                }
                yPos += 20;
                isAlternate = !isAlternate;
            }

            // Totals row background
            dc.DrawRectangle(Brushes.DarkGray, null, new Rect(40, yPos - 2, pageWidth - 80, 20));

            string[] totalsRow = {
        "Total", "", totalBagCount.ToString(), totalBoxCount.ToString(), totalLeafWeight.ToString(),
        totalWater.ToString(), totalMorapuwata.ToString(), totalThambimata.ToString(), totalReject.ToString(),
        totalBagWeight.ToString(), totalDalu.ToString()
    };

            for (int i = 0; i < totalsRow.Length; i++)
            {
                dc.DrawText(
                    new FormattedText(totalsRow[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, brush, pixelsPerDip),
                    new Point(headerPositions[i], yPos));
            }

            yPos += 40;

            // Signature area
            // Signature area
            double signY = yPos + 60;

            // Align the signature line to the left (starting at position 50)
            dc.DrawLine(new Pen(brush, 1), new Point(50, signY), new Point(350, signY)); // Signature line

            // Align the "Authorised by" text to the left (starting at position 50)
            dc.DrawText(
                new FormattedText("Authorised by", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip),
                new Point(50, signY + 5)); // Adjusted to the same x-coordinate

        }
    }
}

