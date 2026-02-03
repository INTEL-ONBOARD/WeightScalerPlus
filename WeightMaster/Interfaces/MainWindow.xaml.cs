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
using System.Windows.Shapes;
using WeightMaster.utils;
using WeightMaster.Config;
using System.Data;


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
        public AppConfig _appConfig;

        private bool scalerPassedZero = false;


        //to store the user email(to include it to api v2 calls)
        private string userEmail = "";
        private string loginUsername = ""; //to user globally (for admin frame currently)

        //for Enter and Esc navigation
        private int currentStep_st1 = 0;
        private const int maxStep_st1 = 7;
        private int currentStep_st2 = 0;
        private const int maxStep_st2 = 6;


        //public int nSacks_st1 = 0;
        //public int nBoxes_st1 = 0;
        private double singleBoxWeight = 3.5;

        //settings and admin navigation logic
        private bool isFromStation = false;


        //___________________st1 global variables____________________________________________________________________________________________|

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


        private int finalNBoxes_st1 = 0;
        private int finalNSacks_st1 = 0;



        private int finalAvailableGoldenLeafWeight_st1 = 0;
        private int finalAvailableNormalLeafWeight_st1 = 0;
        //private int finalAvailableLeafWeight = 0;

        //___________________st2 global variables____________________________________________________________________________________________|
        //stores multiple rounds of st1 transactionData from a single member to sum them up and insert to finalTransaction data seperately
        private List<TransactionLogBlockModel> currentMemberDetailsList_st2 = new List<TransactionLogBlockModel>();
        //used to send the summed up station1 transactionData directly into station2 finalTransacitonData if wanted.
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


        //admin global variables_______________________________________________________________________________
        //default date if no date was selected at first
        string reportDate = DateTime.Now.ToString("yyyy-MM-dd");



        //API v2 global variables______________________________________________________________________________

        //st1
        memDbLog memberData_st1 = new memDbLog(); //to use at both new and old apis
        GreenLeafPostModel greenLeafPostModel_st1 = new GreenLeafPostModel();

        //st2
        memDbLog memberData_st2 = new memDbLog(); //to use at both new and old apis
        GreenLeafPostModel greenLeafPostModel_st2 = new GreenLeafPostModel();

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
            _appConfig = new AppConfig();
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

            TopbarCustomerLocation.Text = _appConfig.branchName;

            txtBillDate_settings.Text = reportDate;
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

        List<LineBlockModel> lineMasterData = null;
        List<String> supervisorData = null;

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {

            //await _consoleHandler.cloudsync();
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
                            lineNameCmb_admin.Items.Add(lineMaster.LineName);
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
                    //System.Diagnostics.Debug.WriteLine("> fetching supervisor data");
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
            string passwordVis = VisiblePasswordTextBox.Text; //because visible password maintains on a different textbox
            string username = "";
            try
            {
                statusLabel.Content = "Logging in...";
                username = await _consoleHandler.loginUser(email, password);
                if (username.Equals("unknown"))
                { //because visible password maintains on a different textbox
                    username = await _consoleHandler.loginUser(email, passwordVis);
                }
                loginUsername = username;
            }
            catch (Exception ex)
            {
                statusLabel.Content = $"Login failed: {ex.Message}";
            }



            //clear customer completion table upon login
            CustomerCompletionRowPanel.Children.Clear();


            if (!username.Equals("unknown") /*|| true*/)
            {
                userEmail = email;
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
                        AdminFrame.Visibility = Visibility.Collapsed;
                        //change topbar text
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
                        AdminFrame.Visibility = Visibility.Collapsed;

                        //change topbar text";
                        TopBarStationName.Text = "ගෝනි කිරීම";
                        Station2Frame.Focus();

                        //opening customer window
                        //OpenCustomerWindow();
                    }
                    else if (clickedButton.Name == "LoginAdminButton")
                    {
                        //validate admin to access
                        if (!UsernameTextBox.Text.Equals("admin@slt.lk"))
                        {
                            MessageBox.Show(UsernameTextBox.Text + " හට ප්‍රධාන පාලක අවසර නැත");
                            return;
                        }
                        if (!VisiblePasswordTextBox.Text.Equals("admin@1234"))
                        {
                            MessageBox.Show(" නිවැරදි ප්‍රධාන පාලක මුරපදය ඇතුලත් කරන්න");
                            return;
                        }

                        //load the username as the leaf weight officer
                        weightLeafOfficerTxt_admin.Text = username;

                        //PageAdmin.Visibility = Visibility.Visible;
                        LoginFrame.Visibility = Visibility.Collapsed;
                        StationMainFrame.Visibility = Visibility.Visible;
                        Station2Frame.Visibility = Visibility.Visible;
                        Station1Frame.Visibility = Visibility.Collapsed;
                        AdminFrame.Visibility = Visibility.Visible;

                        TopBarStationName.Text = "ප්‍රධා න පා ලක";
                        reportDateTxt_admin.Text = reportDate;
                    }
                }

                //clearing login textboxes otherwise there are visible even after a logout
                UsernameTextBox.Text = "";
                PasswordBoxControl.Password = "";
                VisiblePasswordTextBox.Text = "";

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
        private async void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsFrame.Visibility = Visibility.Visible;
            Station1Frame.Visibility = Visibility.Collapsed;
            //button visibility logic
            ApplicationSettingsButton.Opacity = 0.6;
            GeneralSettingsButton.Opacity = 1.0;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            GeneralSettingsButtonRightArrow.Visibility = Visibility.Visible;

            // Show/hide sections
            GeneralSettingsSection.Visibility = Visibility.Visible;
            ApplicationSettingsSection.Visibility = Visibility.Hidden;
            TransactionViewSection.Visibility = Visibility.Hidden;
            LineSummarySection.Visibility = Visibility.Hidden;

            HomeButton.Visibility = Visibility.Visible;
            SettingsButton.Visibility = Visibility.Collapsed;

            //load transport bills data to table
            var bills = await _consoleHandler.getAllTransportBills();
            //MessageBox.Show(bills.Count.ToString());
            CustomerBillRowPanel.Children.Clear();
            foreach (TransportBillBlockModel bill in bills)
            {
                int idx = bills.IndexOf(bill) + 1;
                CustomerBillTableRow cr1 = new CustomerBillTableRow(idx.ToString(), bill.billNo, bill.lineName);
                CustomerBillRowPanel.Children.Add(cr1);
            }
        }

        private void HomeButtonClick(object sender, RoutedEventArgs e)
        {
            if (isFromStation)
            {
                //MessageBox.Show("is from station");
                AdminFrame.Visibility = Visibility.Hidden;
            }
            StationMainFrame.Visibility = Visibility.Visible;
            Station2Frame.Visibility = Visibility.Visible;
            SettingsFrame.Visibility = Visibility.Collapsed;
            //button visibility logic
            //Console.Beep();
            SettingsButton.Visibility = Visibility.Visible;
            HomeButton.Visibility = Visibility.Collapsed;

            isFromStation = false;

        }

        private void LogoutButtonClick(object sender, RoutedEventArgs e)
        {
            //IntroFrame.Visibility = Visibility.Visible;
            //LoginFrame.Visibility = Visibility.Visible;
            StationMainFrame.Visibility = Visibility.Collapsed;
            Station1Frame.Visibility = Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;
            AdminFrame.Visibility = Visibility.Collapsed;

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

            finalNBoxes_st1 = 0;
            finalNSacks_st1 = 0;

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
            System.Diagnostics.Debug.WriteLine("system check 2");
            StartupTheAppAsync();
        }


        //station1 frame_______________________________________________________________________________________________________________________

        private async void barcodeTxt_st1_TextChanged(object sender, TextChangedEventArgs e)
        {
            memberId_st1 = barcodeTxt_st1.Text;
            //converts the member id to a 05-digit number by adding remaining zeros to the left.
            memberId_st1 = barcodeTxt_st1.Text.PadLeft(5, '0');
            //to clear out previous values by assigning null
            string memberName = null;
            //greenLeafPostModel_st1 = null; //this was done 'cause i need to call getDatabyMemberiDandDateSingle() in confirm button w/o affecting the turn table flow(by preventing barcodeTxt="" at confirm button click)
            try
            {
                memberName = await _consoleHandler.GetMemberName(memberId_st1);
                customerNameTxt_st1.Text = memberName;
                if (memberName.Equals("No name with initials found"))
                {
                    customerNameTxt_st1.Text = "-";
                }
                memberData_st1 = await _consoleHandler.getMember(memberId_st1);

                //new api (if exists(not null), do an update after adding the new transaction at confirm button click evt)
                greenLeafPostModel_st1 = await _consoleHandler.getDatabyMemberiDandDateSingle(memberId_st1, DateTime.Now.ToString("yyyy-MM-dd"));
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

            finalNBoxes_st1 = 0;
            finalNSacks_st1 = 0;

            finalAvailableGoldenLeafWeight_st1 = 0;
            finalAvailableNormalLeafWeight_st1 = 0;

            //clear member wise table data and clear it's total row
            MemberTurnTablePanel_st1.Children.Clear();
            rowNBoxes_st1.Text = "0";
            rowNSacks_st1.Text = "0";
            rowGreenLeafWeights_st1.Text = "0";
            rowGoldenLeafWeights_st1.Text = "0";
            rowTotalLeafWeights_st1.Text = "0"; //only sacks and boxes deducted



            //to repopulate the member turn table(same as in line name dropdown)

            //clear the linewise table before entering new data
            LineTablePanel_st1.Children.Clear();
            List<TransactionLogBlockModel> roundData = null;
            try
            {
                var result = lineMasterData.FirstOrDefault(item => item.LineName == lineNameCmb_st1.SelectedItem.ToString());
                if (result == null)
                { return; }
                lineMasterNameLbl_st1.Text = result.LineMaster;
                //roundData is used tot populate a table
                //if empty string, include full list
                if (memberId_st1.ToString() == "00000" || memberId_st1.ToString() == "" || barcodeTxt_st1.ToString() == "")
                {
                    //returns the full list
                    roundData = await _consoleHandler.getDataByFilter(result.LineName.ToString());
                }
                //if not empty string, filter list by barcode details string
                else
                {
                    roundData = null;
                    List<TransactionLogBlockModel> tempRoundData = await _consoleHandler.getDataByFilter(result.LineName.ToString());
                    roundData = tempRoundData.Where(t => t.barcode_details == memberId_st1 || t.barcode_details == barcodeTxt_st1.ToString()).ToList();
                }

                //populate the data in a table
                if (roundData.Any())
                {
                    // to assign into total values row
                    int rowNBoxes = 0;
                    int rowNSacks = 0;
                    int rowGoldenLeafWeight = 0;
                    int rowNormalLeafWeight = 0;
                    int rowTotalLeafWeight = 0;

                    // dictionary to count occurrences for each barcode_details (used to generate per-member round numbers)
                    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var transaction in roundData)
                    {
                        // normalize barcode key and guard against null
                        string barcodeKey = transaction.barcode_details?.ToString() ?? string.Empty;

                        // increment occurrence count for this barcode and use it as the round number
                        if (!counts.TryGetValue(barcodeKey, out int currentCount))
                        {
                            currentCount = 0;
                        }
                        currentCount++;
                        counts[barcodeKey] = currentCount;

                        int roundNo = currentCount;

                        // create row — ensure you pass strings if your constructor expects strings
                        Station1LineTableRow lr1 = new Station1LineTableRow(
                            roundNo.ToString(),
                            barcodeKey,
                            transaction.bag_count.ToString(),
                            transaction.box_count.ToString(),
                            transaction.total_gold_leaf_weight.ToString(),
                            transaction.actual_nomal_leaf_weight.ToString(),
                            (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight).ToString()
                        );

                        LineTablePanel_st1.Children.Add(lr1);

                        // accumulate totals
                        rowNBoxes += transaction.box_count;
                        rowNSacks += transaction.bag_count;
                        rowGoldenLeafWeight += transaction.total_gold_leaf_weight;
                        rowNormalLeafWeight += transaction.actual_nomal_leaf_weight;
                        rowTotalLeafWeight += (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight);
                    }

                    // assign total column values to the total values row
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
        private async void lineNameCmb_st1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //clear the linewise table before entering new data
            LineTablePanel_st1.Children.Clear();
            string searchLineName = lineNameCmb_st1.SelectedItem.ToString(); // replace with the line name you're searching for
            var result = lineMasterData.FirstOrDefault(item => item.LineName == searchLineName);

            //get transport bill by line name
            var transportBill = await _consoleHandler.getTransportBillNoByLineName(searchLineName);
            billNoTxt_st1.Text = transportBill ?? "-";

            if (result != null)
            {
                List<TransactionLogBlockModel> roundData = null;
                lineMasterNameLbl_st1.Text = result.LineMaster;
                try
                {
                    //roundData is used tot populate a table
                    //if empty string, include full list
                    if (memberId_st1.ToString() == "00000" || memberId_st1.ToString() == "" || barcodeTxt_st1.ToString() == "")
                    {
                        //returns the full list
                        roundData = await _consoleHandler.getDataByFilter(result.LineName.ToString());
                    }
                    //if not empty string, filter list by barcode details string
                    else
                    {
                        roundData = null;
                        List<TransactionLogBlockModel> tempRoundData = await _consoleHandler.getDataByFilter(result.LineName.ToString());
                        roundData = tempRoundData.Where(t => t.barcode_details == memberId_st1 || t.barcode_details == barcodeTxt_st1.ToString()).ToList();
                    }

                    //populate the data in a table
                    if (roundData.Any())
                    {
                        // to assign into total values row
                        int rowNBoxes = 0;
                        int rowNSacks = 0;
                        int rowGoldenLeafWeight = 0;
                        int rowNormalLeafWeight = 0;
                        int rowTotalLeafWeight = 0;

                        // dictionary to count occurrences for each barcode_details (used to generate per-member round numbers)
                        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                        foreach (var transaction in roundData)
                        {
                            // normalize barcode key and guard against null
                            string barcodeKey = transaction.barcode_details?.ToString() ?? string.Empty;

                            // increment occurrence count for this barcode and use it as the round number
                            if (!counts.TryGetValue(barcodeKey, out int currentCount))
                            {
                                currentCount = 0;
                            }
                            currentCount++;
                            counts[barcodeKey] = currentCount;

                            int roundNo = currentCount;

                            // create row — ensure you pass strings if your constructor expects strings
                            Station1LineTableRow lr1 = new Station1LineTableRow(
                                roundNo.ToString(),
                                barcodeKey,
                                transaction.bag_count.ToString(),
                                transaction.box_count.ToString(),
                                transaction.total_gold_leaf_weight.ToString(),
                                transaction.actual_nomal_leaf_weight.ToString(),
                                (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight).ToString()
                            );

                            LineTablePanel_st1.Children.Add(lr1);

                            // accumulate totals
                            rowNBoxes += transaction.box_count;
                            rowNSacks += transaction.bag_count;
                            rowGoldenLeafWeight += transaction.total_gold_leaf_weight;
                            rowNormalLeafWeight += transaction.actual_nomal_leaf_weight;
                            rowTotalLeafWeight += (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight);
                        }

                        // assign total column values to the total values row
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
                lineMasterNameLbl_st1.Text = "-";
            }
        }

        private void triggerMechanism(object sender, TextChangedEventArgs e)
        {
            //MessageBox.Show("current step: "+currentStep_st1.ToString());

            if (weightScalerStatus_st1 == null || weightScalerValTxt_st1 == null)
                return;

            if (!double.TryParse(weightScalerValTxt_st1.Text, out double scalerWeight) || scalerWeight < 0)
                scalerWeight = 0;

            //if scaler button is enabled(after row confirm)
            if (weightScalerConfirmBtn_st1.IsEnabled == false && scalerWeight <= 0/*&& scalerWeight==scalerRoundedWeight_st1*/ /*&& weightScalerStatus_st1.Text.Equals("සමබරයි")*/)
            {
                //MessageBox.Show("passed zero");
                scalerPassedZero = true;
            }
            //if weight scaler value has passed zero, enable scaler confirm button
            // and make sure to verify current step b/w after confirm add button click and before the scaler button click to avoid pre-rebounce
            if (scalerPassedZero == true && currentStep_st1 == 1 && weightScalerStatus_st1.Text.Equals("සමබරයි"))
            {
                //MessageBox.Show("scaler passed zero and current step is 1: button enabled");
                weightScalerConfirmBtn_st1.IsEnabled = true;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));  // green(enabled)
            }
            //if weight scaler value has not passed zero yet, disable scaler confirm button
            else
            {
                //MessageBox.Show("scaler has not passed zero || current step is not 1 || scaler is not balanced");
                weightScalerConfirmBtn_st1.IsEnabled = false;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBDA3"));  // Gray out(disabled)
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

            // Check if the scaler value has passed zero to make sure scaler confirm button stays disabled until it has passed zero 
            if (!scalerPassedZero /*&& weightScalerStatus_st1.Text.Equals("සමබරයි")*/)
            {
                //MessageBox.Show("not passed zero: change blocked");
                weightScalerConfirmBtn_st1.IsEnabled = false;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBDA3"));
                return;
            }
            //now scaler has passed zero by skipping above if statement.
            //ignore processes except step 1 when zero passes
            if ((scalerVal > 0 && weightScalerStatus_st1.Text.Equals("සමබරයි")) && currentStep_st1 == 1)
            {
                //MessageBox.Show("passed zero: change allowed: "+ weightScalerStatus_st1.Text);
                weightScalerConfirmBtn_st1.IsEnabled = true;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            }
            else
            {
                //MessageBox.Show("passed zero: but scaler is not balanced");
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

                //checked in weight value textChange event
                scalerPassedZero = false;
                //disable the confirm button right after.(now this enables when scaler value is zero again in the weight value textChange event)
                weightScalerConfirmBtn_st1.IsEnabled = false;
                weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBDA3"));  // Gray out(disabled)




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
                //MessageBox.Show("done");
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

            //this was added to go back to step 1 and enable scaler confirm button because,
            // this is the only way to get a new scaler value without confirming the transaction
            currentStep_st1 = 1;


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

            if (barcodeTxt_st1.Text.Equals(""))
            {
                MessageBox.Show("සාමාජික අංකය ඇතුලත් කරන්න");
                loadingDataInputBorder_st1.Visibility = Visibility.Hidden;
                currentStep_st1 = currentStep_st1 - 1;
                ShowCurrentStep();
                return;
            }


            //clear and repopulate table rows(test) after successful update
            if (!acceptedLeafWeightTxt_st1.Text.Equals("") && !normalLeafWeightTxt_st1.Text.Equals("") && ((!nSacksTxt_st1.Text.Equals("") && nBoxesTxt_st1.Text.Equals("")) || (nSacksTxt_st1.Text.Equals("") && !nBoxesTxt_st1.Text.Equals(""))))
            {
                //these are used to both updating the db and incrementing total valles for the table
                //float.TryParse(weightScalerValTxt_st1.Text, out float weightScalerValue);
                float weightScalerValue = (float)scalerRoundedWeight_st1;
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
                        company = _appConfig.branchName, //මොරවක්කෝරලේ තේ කම්හල || නව ඇලන්වැලි තේ කම්හල || කෝප්කෝලා තේ කම්හල //BRANCHCHANGE
                        leaf_weight_officer = weightLeafOfficerTxt_st1.Text,
                        superviosr = supervisor_st1,
                        barcode_details = memberId_st1,
                        name_with_initials = customerNameTxt_st1.Text,
                        phone_number = "123-456-7890",
                        date = DateTime.Now.ToString("yyyy-MM-dd"),
                        box_count = nBoxes,
                        bag_count = nSacks,
                        real_value = (float)Math.Round(weightScalerValue, 2),
                        maximum_nomal_leaf_weight = (int)scalerRoundedWeight_st1 - (int)Math.Ceiling(nBoxes * singleBoxWeight), //from now on, this will be used to calculate the box weight in station 2

                        total_leaf_weight = (int)currentNormalLeafWeight_st1 + (int)currentGoldenLeafWeight_st1,
                        actual_nomal_leaf_weight = (int)currentNormalLeafWeight_st1,
                        total_gold_leaf_weight = (int)currentGoldenLeafWeight_st1,

                        water = wateredWeight,
                        morapuwata = maturedWeight,
                        thambimata = spoiledWeight,
                        reject = rejectedWeight,
                        box_weight = (int)Math.Ceiling(nBoxes * singleBoxWeight), //
                        final_green_leaf_count = availableNormalLeafWeight,
                        final_gold_leaf_count = availableGoldenLeafWeight
                    };

                    // Call the service to add the transaction
                    _isSuccess = await _consoleHandler.AddTransactionAsync(newTransaction);

                    TransportBillBlockModel t1 = new TransportBillBlockModel
                    {
                        billNo = billNoTxt_st1.Text,
                        dateCreated = DateTime.Now.ToString("yyyy-MM-dd"),
                        lineName = lineName_st1,
                    };
                    _consoleHandler.addTransportBill(t1);


                    //----------------------------------------------------------------------------------------------------------------------|

                    //CHANGE: instead of checking just existing memberID, check both memberID and TransportLine
                    //MessageBox.Show(memberId_st1);
                    //MessageBox.Show(lineNameCmb_st1.SelectedValue.ToString());
                    List<GreenLeafPostModel> postModels = await _consoleHandler.getAllpostsbyFilteringExtended(memberId_st1, lineNameCmb_st1.SelectedValue.ToString());

                    //if (postModels == null) { MessageBox.Show("null"); }
                    //if (postModels.Count == 0) { MessageBox.Show("count is zero"); }
                    foreach (var post in postModels)
                    {
                        //string message = $@"Green Leaf Post Details:
                        //                    ID: {post.id}
                        //                    Leaf Handover Date: {post.leaf_handover_date}
                        //                    Factory: {post.factory}
                        //                    Transport Line Name: {post.transportlinename}
                        //                    Transport Agent: {post.transportagent}
                        //                    Leaf Weight Officer: {post.leaf_weight_officer}
                        //                    Supervisor: {post.supervisor}
                        //                    Member Number: {post.membernumber}
                        //                    Pre-Member Number: {post.premembernumber ?? "N/A"}
                        //                    Bag Count: {post.bag_count}
                        //                    Box Count: {post.box_count}
                        //                    Real Weight: {post.real_weight}
                        //                    Total Weight: {post.total_weight}
                        //                    Normal Leaf Weight: {post.nomal_leaf_weight}
                        //                    Gold Leaf Weight: {post.gold_leaf_weight}
                        //                    Wathurata: {post.wathurata}
                        //                    Morapuwata: {post.morapuwata}
                        //                    Thambimata: {post.thambimata}
                        //                    Rejected: {post.rejected}
                        //                    Bag Weight: {post.bag_weight}
                        //                    Box Weight: {post.box_weight}
                        //                    Final Green Leaf Count: {post.final_green_leaf_count}
                        //                    Final Gold Leaf Count: {post.final_gold_leaf_count}
                        //                    Created User: {post.created_user}
                        //                    Updated User: {post.updated_user}";
                        //MessageBox.Show(message, "Green Leaf Post Details");
                    }
                    GreenLeafPostModel postModelSumFromLine = new GreenLeafPostModel();
                    if (postModels == null || postModels.Count == 0)
                    {
                        //MessageBox.Show("This list is empty");
                    }
                    else
                    {
                        //MessageBox.Show($"List has {postModels.Count} items.");
                        postModelSumFromLine = BlockModelConverter.Sum(postModels);
                    }
                    //string message1 = $@"Green Leaf Post Details:
                    //                        ID: {postModelSumFromLine.id}
                    //                        Leaf Handover Date: {postModelSumFromLine.leaf_handover_date}
                    //                        Factory: {postModelSumFromLine.factory}
                    //                        Transport Line Name: {postModelSumFromLine.transportlinename}
                    //                        Transport Agent: {postModelSumFromLine.transportagent}
                    //                        Leaf Weight Officer: {postModelSumFromLine.leaf_weight_officer}
                    //                        Supervisor: {postModelSumFromLine.supervisor}
                    //                        Member Number: {postModelSumFromLine.membernumber}
                    //                        Pre-Member Number: {postModelSumFromLine.premembernumber ?? "N/A"}
                    //                        Bag Count: {postModelSumFromLine.bag_count}
                    //                        Box Count: {postModelSumFromLine.box_count}
                    //                        Real Weight: {postModelSumFromLine.real_weight}
                    //                        Total Weight: {postModelSumFromLine.total_weight}
                    //                        Normal Leaf Weight: {postModelSumFromLine.nomal_leaf_weight}
                    //                        Gold Leaf Weight: {postModelSumFromLine.gold_leaf_weight}
                    //                        Wathurata: {postModelSumFromLine.wathurata}
                    //                        Morapuwata: {postModelSumFromLine.morapuwata}
                    //                        Thambimata: {postModelSumFromLine.thambimata}
                    //                        Rejected: {postModelSumFromLine.rejected}
                    //                        Bag Weight: {postModelSumFromLine.bag_weight}
                    //                        Box Weight: {postModelSumFromLine.box_weight}
                    //                        Final Green Leaf Count: {postModelSumFromLine.final_green_leaf_count}
                    //                        Final Gold Leaf Count: {postModelSumFromLine.final_gold_leaf_count}
                    //                        Created User: {postModelSumFromLine.created_user}
                    //                        Updated User: {postModelSumFromLine.updated_user}";
                    //MessageBox.Show(message1, "Final Green Leaf Post Details");
                    //call new api v2
                    //save only if the table row doesn't already exists
                    //if (greenLeafPostModel_st1 == null)
                    if (postModels == null || postModels.Count == 0)
                    {
                        //MessageBox.Show("null was selected: creating new");
                        var newGRPM = new GreenLeafPostModel
                        {
                            id = 0,
                            leaf_handover_date = DateTime.Now.ToString("yyyy-MM-dd"),
                            factory = _appConfig.branchId, //coop:1 || coop-cola:2 || allan-valley:3  //BRANCHCHANGE

                            transportlinename = lineName_st1,
                            transportagent = lineMasterNameLbl_st1.Text,
                            leaf_weight_officer = weightLeafOfficerTxt_st1.Text,
                            supervisor = supervisor_st1,
                            membernumber = memberId_st1,
                            premembernumber = memberData_st1.CustomPreMemberNum,

                            bag_count = nSacks,
                            box_count = nBoxes,

                            real_weight = Math.Round(weightScalerValue, 2),
                            //total_weight = (int)currentNormalLeafWeight_st1 + (int)currentGoldenLeafWeight_st1,
                            total_weight = (int)currentAcceptedLeafWeight_st1 + (int)Math.Ceiling(nBoxes * singleBoxWeight),
                            nomal_leaf_weight = (int)currentNormalLeafWeight_st1 + (int)Math.Ceiling(nBoxes * singleBoxWeight),
                            gold_leaf_weight = (int)currentGoldenLeafWeight_st1,

                            wathurata = wateredWeight,
                            morapuwata = maturedWeight,
                            thambimata = spoiledWeight,
                            rejected = rejectedWeight,

                            bag_weight = 0,
                            box_weight = (int)Math.Ceiling(nBoxes * singleBoxWeight),

                            final_green_leaf_count = availableNormalLeafWeight,
                            final_gold_leaf_count = availableGoldenLeafWeight,

                            created_user = userEmail,
                            updated_user = ""
                        };
                        //MessageBox.Show("total_weight: "+newGRPM.total_weight.ToString());
                        //MessageBox.Show("final_green_leaf_count: " + newGRPM.final_green_leaf_count.ToString());



                        await _consoleHandler.SaveData(newGRPM);
                    }
                    else //add the current values to the existing record and update it instead of adding a new one
                    {
                        //MessageBox.Show("not null was selected: updating existing");
                        //MessageBox.Show("is null");
                        var newGRPM = new GreenLeafPostModel
                        {
                            id = 0,
                            leaf_handover_date = DateTime.Now.ToString("yyyy-MM-dd"),
                            factory = _appConfig.branchId, //coop:1 || coop-cola:2 || allan-valley:3  //BRANCHCHANGE

                            transportlinename = lineName_st1,
                            transportagent = lineMasterNameLbl_st1.Text,
                            leaf_weight_officer = weightLeafOfficerTxt_st1.Text,
                            supervisor = supervisor_st1,
                            membernumber = memberId_st1,
                            premembernumber = memberData_st1.CustomPreMemberNum,

                            bag_count = nSacks + postModelSumFromLine.bag_count,
                            box_count = nBoxes + postModelSumFromLine.box_count,

                            real_weight = Math.Round(weightScalerValue, 2) + postModelSumFromLine.real_weight,
                            total_weight = (int)acceptedLeafWeight + postModelSumFromLine.total_weight + (int)Math.Ceiling(nBoxes * singleBoxWeight),

                            nomal_leaf_weight = (int)currentNormalLeafWeight_st1 + postModelSumFromLine.nomal_leaf_weight + (int)Math.Ceiling(nBoxes * singleBoxWeight),
                            gold_leaf_weight = (int)currentGoldenLeafWeight_st1 + postModelSumFromLine.gold_leaf_weight,

                            wathurata = wateredWeight + postModelSumFromLine.wathurata,
                            morapuwata = maturedWeight + postModelSumFromLine.morapuwata,
                            thambimata = spoiledWeight + postModelSumFromLine.thambimata,
                            rejected = rejectedWeight + postModelSumFromLine.rejected,

                            bag_weight = 0 + postModelSumFromLine.bag_weight,
                            box_weight = (int)Math.Ceiling(nBoxes * singleBoxWeight) + postModelSumFromLine.box_weight,

                            final_green_leaf_count = availableNormalLeafWeight + postModelSumFromLine.final_green_leaf_count,
                            final_gold_leaf_count = availableGoldenLeafWeight + postModelSumFromLine.final_gold_leaf_count,

                            created_user = userEmail,
                            updated_user = ""
                        };
                        //MessageBox.Show("total_weight: " + newGRPM.total_weight.ToString());
                        //MessageBox.Show("final_green_leaf_count: " + newGRPM.final_green_leaf_count.ToString());


                        bool success_update = await _consoleHandler.UpdateData(postModelSumFromLine.id, newGRPM);
                        //MessageBox.Show(success_update.ToString());
                    }


                    //new api (if exists(not null), do an update after adding the new transaction)
                    //this was called here after saveData() because i can't clear barcodeTxt. this will clear the turn table which runs w/o a direct api.
                    greenLeafPostModel_st1 = await _consoleHandler.getDatabyMemberiDandDateSingle(memberId_st1, DateTime.Now.ToString("yyyy-MM-dd"));

                    // enable and disable confirm buttons
                    confirmAddRowButton_st1.IsEnabled = false;
                    confirmAddRowButtonBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B4B4B4"));  // Gray out


                    //newly added: verify if the weight scaler value is zero or not for this existing button disability and color change
                    if (scalerPassedZero == true)
                    {
                        //MessageBox.Show("Weight Scaler has passed zero before this confirm button evt: scal button enabled");
                        weightScalerConfirmBtn_st1.IsEnabled = true;
                        weightScalerConfirmBtnBorder_st1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled)
                    }
                    else
                    {
                        //MessageBox.Show("Weight Scaler has not passed zero before this confirm button evt: scal button disabled");
                    }

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

                    finalNBoxes_st1 += nBoxes;
                    finalNSacks_st1 += nSacks;


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

                    finalNBoxes_st1 += nBoxes;
                    finalNSacks_st1 += nSacks;

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
                //updating the linewise table to show the confirmed transaction by member id
                try
                {
                    List<TransactionLogBlockModel> roundData = null;
                    //if empty string, include full list
                    if (memberId_st1.ToString() == "00000" || memberId_st1.ToString() == "" || barcodeTxt_st1.ToString() == "")
                    {
                        //returns the full list
                        roundData = await _consoleHandler.getDataByFilter(lineNameCmb_st1.SelectedItem.ToString());
                    }
                    //if not empty string, filter list by barcode details string
                    else
                    {
                        roundData = null;
                        List<TransactionLogBlockModel> tempRoundData = await _consoleHandler.getDataByFilter(lineNameCmb_st1.SelectedItem.ToString());
                        roundData = tempRoundData.Where(t => t.barcode_details == memberId_st1 || t.barcode_details == barcodeTxt_st1.ToString()).ToList();
                    }
                    if (roundData.Any())
                    {
                        // to assign into total values row
                        int rowNBoxes = 0;
                        int rowNSacks = 0;
                        int rowGoldenLeafWeight = 0;
                        int rowNormalLeafWeight = 0;
                        int rowTotalLeafWeight = 0;

                        // dictionary to count occurrences for each barcode_details (used to generate per-member round numbers)
                        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                        //populate lines by barcode
                        foreach (var transaction in roundData)
                        {
                            // normalize barcode key and guard against null
                            string barcodeKey = transaction.barcode_details?.ToString() ?? string.Empty;

                            // increment occurrence count for this barcode and use it as the round number
                            if (!counts.TryGetValue(barcodeKey, out int currentCount))
                            {
                                currentCount = 0;
                            }
                            currentCount++;
                            counts[barcodeKey] = currentCount;

                            int roundNo = currentCount;

                            // create row
                            Station1LineTableRow lr1 = new Station1LineTableRow(
                                roundNo.ToString(),
                                barcodeKey,
                                transaction.bag_count.ToString(),
                                transaction.box_count.ToString(),
                                transaction.total_gold_leaf_weight.ToString(),
                                transaction.actual_nomal_leaf_weight.ToString(),
                                (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight).ToString()
                            );

                            LineTablePanel_st1.Children.Add(lr1);

                            // accumulate totals
                            rowNBoxes += transaction.box_count;
                            rowNSacks += transaction.bag_count;
                            rowGoldenLeafWeight += transaction.total_gold_leaf_weight;
                            rowNormalLeafWeight += transaction.actual_nomal_leaf_weight;
                            rowTotalLeafWeight += (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight);
                        }

                        // assign total column values to the total values row
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
            string memberId = barcodeTxt_st2.Text.PadLeft(5, '0');

            //clear member turn table for the next member(this is hidden currently)
            MemberTurnTablePanel_st2.Children.Clear();
            //clear all weight deduction rounds for the next id
            addedRoundList.Clear();

            ////clear member wise table data and clear it's total row
            //MemberTurnTablePanel_st1.Children.Clear();
            //rowNBoxes_st1.Text = "0";
            //rowNSacks_st1.Text = "0";
            //rowGreenLeafWeights_st1.Text = "0";
            //rowGoldenLeafWeights_st1.Text = "0";
            //rowTotalLeafWeights_st1.Text = "0"; //only sacks and boxes deducted

            string memberName = "";
            try
            {
                memberName = await _consoleHandler.GetMemberName(memberId);
                //either a new parameter should be added or i should filter them to get data by line
                greenLeafPostModel_st2 = await _consoleHandler.getDatabyMemberiDandDateSingle(memberId, DateTime.Now.ToString("yyyy-MM-dd"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Get member Exception: " + ex.Message);
            }

            //System.Diagnostics.Debug.WriteLine(barcodeTxt_st2 + " as " + memberId + ": " + memberName);
            customerNameTxt_st2.Text = memberName;
            if (memberName.Equals("No name with initials found"))
            {
                customerNameTxt_st1.Text = "-";
            }


            //clear previous data if there are any
            currentMemberDetailsList_st2.Clear();
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

            //gets the transaction list by id(for testing purposes)
            //currentMemberDetailsList_st2 = await _consoleHandler.getDataByFilter("ඉළුකපිටිය");
            //GetTransactionDataByBarcodeId
            //Either line parameter should be added or it should filter by line
            currentMemberDetailsList_st2 = await _consoleHandler.GetTransactionDataByBarcodeId(memberId);
            //filtering list by line name [auto select the first line name]
            //currentMemberDetailsList_st2 = BlockModelConverter.filterTransactionDataByLine(currentMemberDetailsList_st2, currentMemberDetailsList_st2[0].linename);

            currentMemberDetailsList_st2 = BlockModelConverter.filterTransactionDataByLine(currentMemberDetailsList_st2, lineNameCmb_st2.SelectedItem.ToString());

            if (currentMemberDetailsList_st2.Count != 0)
            {

                currentMemberDetails_st2 = new TransactionLogBlockModel();

                //get member details
                currentMemberDetails_st2.linename = currentMemberDetailsList_st2[0].linename;
                currentMemberDetails_st2.transportagent = currentMemberDetailsList_st2[0].transportagent;
                currentMemberDetails_st2.company = currentMemberDetailsList_st2[0].company;
                currentMemberDetails_st2.leaf_weight_officer = currentMemberDetailsList_st2[0].leaf_weight_officer;
                currentMemberDetails_st2.superviosr = currentMemberDetailsList_st2[0].superviosr;

                currentMemberDetails_st2.barcode_details = currentMemberDetailsList_st2[0].barcode_details;
                currentMemberDetails_st2.name_with_initials = currentMemberDetailsList_st2[0].name_with_initials;
                currentMemberDetails_st2.phone_number = currentMemberDetailsList_st2[0].phone_number;
                currentMemberDetails_st2.date = currentMemberDetailsList_st2[0].date;

                //prepare values to addition by making them zero and 
                currentMemberDetails_st2.box_count = 0;
                currentMemberDetails_st2.bag_count = 0;
                currentMemberDetails_st2.real_value = 0;

                currentMemberDetails_st2.maximum_nomal_leaf_weight = 0;
                currentMemberDetails_st2.total_leaf_weight = 0;
                currentMemberDetails_st2.actual_nomal_leaf_weight = 0;
                currentMemberDetails_st2.total_gold_leaf_weight = 0;

                currentMemberDetails_st2.water = 0;
                currentMemberDetails_st2.morapuwata = 0;
                currentMemberDetails_st2.thambimata = 0;
                currentMemberDetails_st2.reject = 0;

                currentMemberDetails_st2.final_green_leaf_count = 0;
                currentMemberDetails_st2.final_gold_leaf_count = 0;
                //MessageBox.Show($"item count {currentMemberDetailsList_st2.Count}");
                foreach (var st1Round in currentMemberDetailsList_st2)
                {
                    currentMemberDetails_st2.box_count += st1Round.box_count;
                    currentMemberDetails_st2.bag_count += st1Round.bag_count;
                    currentMemberDetails_st2.real_value += st1Round.real_value;

                    currentMemberDetails_st2.maximum_nomal_leaf_weight += st1Round.maximum_nomal_leaf_weight;
                    currentMemberDetails_st2.total_leaf_weight += st1Round.total_leaf_weight;
                    currentMemberDetails_st2.actual_nomal_leaf_weight += st1Round.actual_nomal_leaf_weight;
                    currentMemberDetails_st2.total_gold_leaf_weight += st1Round.total_gold_leaf_weight;

                    currentMemberDetails_st2.water += st1Round.water;
                    currentMemberDetails_st2.morapuwata += st1Round.morapuwata;
                    currentMemberDetails_st2.thambimata += st1Round.thambimata;
                    currentMemberDetails_st2.reject += st1Round.reject;

                    currentMemberDetails_st2.final_green_leaf_count += st1Round.final_green_leaf_count;
                    currentMemberDetails_st2.final_gold_leaf_count += st1Round.final_gold_leaf_count;

                }
            }
            else {/*MessageBox.Show("is null");*/}

            //get user data to populate textboxes and such
            //currentMemberDetails_st2 = await _consoleHandler.GetTransactionData(memberId, memberId);
            if (currentMemberDetails_st2 != null)
            {
                System.Diagnostics.Debug.WriteLine($"Line name: {currentMemberDetails_st2.linename}, Line Name: {currentMemberDetails_st2.Id}, Line Master: {currentMemberDetails_st2.transportagent}");
                //if (memberDetails == null)
                //    MessageBox.Show("member data is null");

                //if (currentMemberDetails_st2 != null)
                //{
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
                //}
                //.Text = "";
                //.Text = "";
                //public int currentAcceptedLeafWeight_st2 = 82;
                //private double currentGoldenLeafWeight_st2 = 0;
                //private double currentNormalLeafWeight_st2 = 0;

                //public double currentTotalDeduction_st2 = 0;

            }
            //System.Diagnostics.Debug.WriteLine("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");


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

            //to repopulate the member turn table(same as in line name dropdown)

            //clear the linewise table before entering new data
            CustomerCompletionRowPanel.Children.Clear();
            List<TransactionLogBlockModel> transactions_notCompleted_st2 = null;
            List<TransactionLogBlockModel> transactions_completed_st2 = null;
            try
            {
                string lineName = lineNameCmb_st2.SelectedItem.ToString();
                var result = lineMasterData.FirstOrDefault(item => item.LineName == lineName);
                if (result == null)
                { return; }
                //lineMasterNameLbl_st1.Text = result.LineMaster;
                //transactions_notCompleted_st2 and transactions_completed_st2 is used to populate a table
                //if empty string, include full list
                if (memberId.ToString() == "00000" || memberId.ToString() == "" || barcodeTxt_st2.ToString() == "")
                {
                    //returns the full list
                    transactions_notCompleted_st2 = await _consoleHandler.getDataByFilter(lineName);
                    transactions_completed_st2 = await _consoleHandler.getCompletedDataByFilter(lineName);
                }
                //if not empty string, filter list by barcode details string
                else
                {
                    transactions_notCompleted_st2 = null;
                    transactions_completed_st2 = null;
                    //show only for the current member id/barcode
                    List<TransactionLogBlockModel> tempRoundData = await _consoleHandler.getDataByFilter(lineName);
                    transactions_notCompleted_st2 = tempRoundData.Where(t => t.barcode_details == memberId || t.barcode_details == barcodeTxt_st2.ToString()).ToList();
                    List<TransactionLogBlockModel> tempRoundData_completed = await _consoleHandler.getCompletedDataByFilter(lineName);
                    transactions_completed_st2 = tempRoundData_completed.Where(t => t.barcode_details == memberId || t.barcode_details == barcodeTxt_st2.ToString()).ToList();

                }

                //populate the data in a table
                if (transactions_notCompleted_st2.Any() || transactions_completed_st2.Any())
                {
                    // to assign into total values row
                    int rowNSacks = 0;
                    int rowGoldenLeafWeight = 0;
                    int rowNormalLeafWeight = 0;
                    int rowTotalLeafWeight = 0;

                    // dictionary to count occurrences for each barcode_details (used to generate per-member round numbers)
                    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    if (transactions_notCompleted_st2 != null)
                    {
                        foreach (var transaction in transactions_notCompleted_st2)
                        {
                            CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), true, transaction.real_value.ToString("F2", CultureInfo.CurrentCulture), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                            CustomerCompletionRowPanel.Children.Add(cctr4);
                            System.Diagnostics.Debug.WriteLine(transaction.barcode_details + " - " + transaction.linename);


                            // accumulate totals row values
                            rowNSacks += transaction.bag_count;
                            rowGoldenLeafWeight += transaction.total_gold_leaf_weight;
                            rowNormalLeafWeight += transaction.actual_nomal_leaf_weight;
                            rowTotalLeafWeight += (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight);
                        }
                    }

                    if (transactions_completed_st2 != null)
                    {
                        foreach (var transaction in transactions_completed_st2)
                        {
                            CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), false, transaction.real_value.ToString("F2", CultureInfo.CurrentCulture), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                            CustomerCompletionRowPanel.Children.Add(cctr4);
                            System.Diagnostics.Debug.WriteLine(transaction.barcode_details + " - " + transaction.linename);

                            // accumulate totals row values
                            rowNSacks += transaction.bag_count;
                            rowGoldenLeafWeight += transaction.total_gold_leaf_weight;
                            rowNormalLeafWeight += transaction.actual_nomal_leaf_weight;
                            rowTotalLeafWeight += (transaction.total_gold_leaf_weight + transaction.actual_nomal_leaf_weight);
                        }
                    }

                    // assign total column values to the total values row
                    lineRowNBagsTxt_st2.Text = rowNSacks.ToString();
                    lineRowTotalLeafWeightsTxt_st2.Text = rowTotalLeafWeight.ToString();
                    lineRowAcceptedLeafWeightsTxt_st2.Text = rowTotalLeafWeight.ToString();
                    lineRowGoldLeafWeightsTxt_st2.Text = rowGoldenLeafWeight.ToString();
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


        private async void lineNameCmb_st2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string memberId_st2 = barcodeTxt_st2.Text.PadLeft(5, '0');

            List<TransactionLogBlockModel> transactions_notCompleted_st2 = null;
            List<TransactionLogBlockModel> transactions_completed_st2 = null;

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

            ////roundData is used tot populate a table
            ////if empty string, include full list
            //if (memberId_st1.ToString() == "00000" || memberId_st1.ToString() == "" || barcodeTxt_st1.ToString() == "")
            //{
            //    //returns the full list
            //    roundData = await _consoleHandler.getDataByFilter(result.LineName.ToString());
            //}
            ////if not empty string, filter list by barcode details string
            //else
            //{
            //    roundData = null;
            //    List<TransactionLogBlockModel> tempRoundData = await _consoleHandler.getDataByFilter(result.LineName.ToString());
            //    roundData = tempRoundData.Where(t => t.barcode_details == memberId_st1 || t.barcode_details == barcodeTxt_st1.ToString()).ToList();
            //}



            try
            {
                CustomerCompletionRowPanel.Children.Clear();
                //shows the table rows for bags waiting to be completed in stations 02
                //if empty string, include full list
                if (memberId_st2.ToString() == "00000" || memberId_st2.ToString() == "" || barcodeTxt_st2.ToString() == "")
                {
                    transactions_notCompleted_st2 = await _consoleHandler.getDataByFilter(lineName);
                    transactions_completed_st2 = await _consoleHandler.getCompletedDataByFilter(lineName);
                }
                else
                {
                    transactions_notCompleted_st2 = null;
                    transactions_completed_st2 = null;
                    //show only for the current member id/barcode
                    List<TransactionLogBlockModel> tempRoundData = await _consoleHandler.getDataByFilter(lineName);
                    transactions_notCompleted_st2 = tempRoundData.Where(t => t.barcode_details == memberId_st2 || t.barcode_details == barcodeTxt_st2.ToString()).ToList();
                    List<TransactionLogBlockModel> tempRoundData_completed = await _consoleHandler.getCompletedDataByFilter(lineName);
                    transactions_completed_st2 = tempRoundData_completed.Where(t => t.barcode_details == memberId_st2 || t.barcode_details == barcodeTxt_st2.ToString()).ToList();

                }

                if (transactions_notCompleted_st2 != null)
                {
                    foreach (var transaction in transactions_notCompleted_st2)
                    {
                        CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), true, transaction.real_value.ToString("F2", CultureInfo.CurrentCulture), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                        CustomerCompletionRowPanel.Children.Add(cctr4);
                        System.Diagnostics.Debug.WriteLine(transaction.barcode_details + " - " + transaction.linename);
                    }
                }

                //shows the table rows for bags that have completed weighting in stations 02
                if (transactions_completed_st2 != null)
                {
                    foreach (var transaction in transactions_completed_st2)
                    {
                        CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), false, transaction.real_value.ToString("F2", CultureInfo.CurrentCulture), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
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

            //get scaler value to only one decimal place.
            double tempScalerWeight = Math.Truncate(scalerWeight * 10) / 10;

            //if the value is more than 2.1, it's rounded to 3.0. if it's less or equal than 2.1, it's rounded to 2.0
            int ceilingValue = ToCustomInt(tempScalerWeight);
            //int ceilingValue = (int)Math.Ceiling(scalerWeight);

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
            if (currentMemberDetails_st2 == null) { MessageBox.Show("member details is null"); }
            if (memberHashMap_st2 == null) { MessageBox.Show("member hash map is null"); }
            string memberId = currentMemberDetails_st2?.barcode_details;

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


            // make sure we have a valid memberId and the dictionary is not null
            if (string.IsNullOrWhiteSpace(memberId) || memberHashMap_st2 == null)
            {
                // nothing to load — either no member selected or hashmap missing
                // MessageBox.Show("No member selected to load last round.");
                return;
            }

            //load the last round
            if (memberHashMap_st2.TryGetValue(memberId, out var weightRounds) && weightRounds.Count > 0)
            {
                var lastItem = weightRounds[weightRounds.Count - 1]; //weightRounds[^1];

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
                totalNSacksTxt_st2.Text = "";


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
                if (acceptedSackWeight <= currNormalLeafWeight)
                {
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
                wieghtScalerConfirmBtnBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // green(enabled)
                confirmAddRowButtonBorder_st2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));  // green(enabled)
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
                        company = _appConfig.branchName,  //මොරවක්කෝරලේ තේ කම්හල || නව ඇලන්වැලි තේ කම්හල || කෝප්කෝලා තේ කම්හල //BRANCHCHANGE
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
                        //real_value = totalWeightSaclaerValue
                        real_value = (float)currentMemberDetails_st2.real_value,  //this was changed to determine the box count and weights in repoets
                    };

                    //to fill out missing st1 round values with dummy data(except Id) summed up in barcode textChanged to fix runLog
                    var DummyFinaltransaction = new FinalTransactionBlockModel
                    {
                        //linename = lineNameCmb_st2.SelectedValue.ToString(),
                        Id = 0,
                        linename = "-",
                        transportagent = "-",
                        company = "-",
                        leaf_weight_officer = "-",

                        superviosr = "-",
                        barcode_details = "-",
                        //barcode_details = currentMemberDetails_st2.barcode_details,
                        name_with_initials = "",
                        phone_number = "0712345678",
                        date = DateTime.Now.ToString("yyyy-MM-dd"),

                        bag_count = 0,

                        maximum_nomal_leaf_weight = 0,
                        total_leaf_weight = 0,
                        actual_nomal_leaf_weight = 0,
                        total_gold_leaf_weight = 0,

                        water = 0,
                        morapuwata = 0,
                        thambimata = 0,
                        reject = 0,

                        bag_weight = 0,

                        final_green_leaf_count = 0,
                        final_gold_leaf_count = 0,
                        real_value = 0
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


                    for (int i = 1; i < currentMemberDetailsList_st2.Count; i++)
                    {
                        _isdone = await _consoleHandler.AddFinalTransactionAsync(DummyFinaltransaction, currentMemberDetails_st2.barcode_details);
                        bool isSuccess = await _consoleHandler.verifyTransactionsCloudCheck();
                    }
                    //MessageBox.Show(totalAcceptedSackWeight.ToString());
                    //call new api v2 (st2)
                    var newGRPM = new GreenLeafPostModel
                    {
                        id = 0,
                        leaf_handover_date = DateTime.Now.ToString("yyyy-MM-dd"),
                        factory = _appConfig.branchId, //coop:1 || coop-cola:2 || allan-valley:3  //BRANCHCHANGE

                        transportlinename = lineName_st2,
                        transportagent = lineMasterNameLbl_st2.Text,
                        leaf_weight_officer = weightLeafOfficerTxt_st2.Text,
                        supervisor = currentMemberDetails_st2.superviosr,
                        membernumber = currentMemberDetails_st2.barcode_details,
                        premembernumber = greenLeafPostModel_st2.premembernumber,

                        bag_count = greenLeafPostModel_st2.bag_count,
                        box_count = greenLeafPostModel_st2.box_count,

                        real_weight = greenLeafPostModel_st2.real_weight,
                        //if cloud server wanted deduct bag weight as well in case, deduct from below
                        total_weight = greenLeafPostModel_st2.total_weight,

                        nomal_leaf_weight = greenLeafPostModel_st2.nomal_leaf_weight,
                        gold_leaf_weight = greenLeafPostModel_st2.gold_leaf_weight,

                        wathurata = finalWateredWeight,
                        morapuwata = finalMaturedWeight,
                        thambimata = finalSpoiledWeight,
                        rejected = finalRejectedWeight,

                        bag_weight = totalAcceptedSackWeight,
                        box_weight = greenLeafPostModel_st2.box_weight,

                        final_green_leaf_count = finalAvailableNormalLeafWeight,
                        final_gold_leaf_count = finalAvailableGoldenLeafWeight,

                        created_user = greenLeafPostModel_st2.created_user,
                        updated_user = userEmail
                    };
                    //MessageBox.Show("total_weight: " + newGRPM.total_weight.ToString());
                    //MessageBox.Show("final_green_leaf_count: " + newGRPM.final_green_leaf_count.ToString());

                    //Supporter.handleEquationSt2(newGRPM, totalAcceptedSackWeight);

                    //MessageBox.Show(newGRPM.bag_weight.ToString());
                    bool passed = await _consoleHandler.UpdateData(greenLeafPostModel_st2.id, newGRPM);
                    //MessageBox.Show(passed.ToString());
                    //await _consoleHandler.cloudsync();

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
                    List<TransactionLogBlockModel> transactions_notCompleted_st2 = null;
                    List<TransactionLogBlockModel> transactions_completed_st2 = null;
                    string memberId_st2 = barcodeTxt_st2.Text.PadLeft(5, '0');
                    //MessageBox.Show("member id: "+memberId_st2);
                    //MessageBox.Show("cleared all records");
                    //shows the table rows for bags waiting to be completed in stations 02
                    if (memberId_st2.ToString() == "00000" || memberId_st2.ToString() == "" || barcodeTxt_st2.ToString() == "")
                    {
                        //MessageBox.Show("getting all records");
                        transactions_notCompleted_st2 = await _consoleHandler.getDataByFilter(lineName);
                        transactions_completed_st2 = await _consoleHandler.getCompletedDataByFilter(lineName);
                    }
                    else
                    {
                        //MessageBox.Show("getting filtered records");
                        transactions_notCompleted_st2 = null;
                        transactions_completed_st2 = null;
                        //show only for the current member id/barcode
                        List<TransactionLogBlockModel> tempRoundData = await _consoleHandler.getDataByFilter(lineName);
                        transactions_notCompleted_st2 = tempRoundData.Where(t => t.barcode_details == memberId_st2 || t.barcode_details == barcodeTxt_st2.ToString()).ToList();
                        List<TransactionLogBlockModel> tempRoundData_completed = await _consoleHandler.getCompletedDataByFilter(lineName);
                        transactions_completed_st2 = tempRoundData_completed.Where(t => t.barcode_details == memberId_st2 || t.barcode_details == barcodeTxt_st2.ToString()).ToList();

                    }
                    //clear existing rows for repopulation
                    CustomerCompletionRowPanel.Children.Clear();
                    //MessageBox.Show("table records cleared");
                    if (transactions_notCompleted_st2 != null)
                    {
                        foreach (var transaction in transactions_notCompleted_st2)
                        {
                            CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), true, transaction.real_value.ToString("F2", CultureInfo.CurrentCulture), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                            CustomerCompletionRowPanel.Children.Add(cctr4);
                            System.Diagnostics.Debug.WriteLine(transaction.barcode_details + " - " + transaction.linename);
                            //MessageBox.Show("pending row added");
                        }
                    }

                    //shows the table rows for bags that have completed weighting in stations 02
                    if (transactions_completed_st2 != null)
                    {
                        foreach (var transaction in transactions_completed_st2)
                        {
                            CustomerCompletionTableRow cctr4 = new CustomerCompletionTableRow(transaction.barcode_details, transaction.name_with_initials, transaction.bag_count.ToString(), false, transaction.real_value.ToString("F2", CultureInfo.CurrentCulture), transaction.total_leaf_weight.ToString(), transaction.final_gold_leaf_count.ToString());
                            CustomerCompletionRowPanel.Children.Add(cctr4);
                            System.Diagnostics.Debug.WriteLine(transaction.barcode_details + " - " + transaction.linename);
                            //MessageBox.Show("completed row added");
                        }
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

        private void ViewReports_Click(object sender, RoutedEventArgs e)
        {
            isFromStation = true;

            //load the username as the leaf weight officer
            weightLeafOfficerTxt_admin.Text = loginUsername;

            //PageAdmin.Visibility = Visibility.Visible;
            //LoginFrame.Visibility = Visibility.Collapsed;
            Station2Frame.Visibility = Visibility.Collapsed;
            Station1Frame.Visibility = Visibility.Visible;
            //StationMainFrame.Visibility = Visibility.Collapsed;
            SettingsFrame.Visibility = Visibility.Hidden;
            AdminFrame.Visibility = Visibility.Visible;

            TopBarStationName.Text = "ප්‍රධා න පා ලක";
            reportDateTxt_admin.Text = reportDate;
        }

        private async void GeneralSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            GeneralSettingsButton.Opacity = 1.0;
            ApplicationSettingsButton.Opacity = 0.6;
            TransactionViewButton.Opacity = 0.6;
            LineSummaryButton.Opacity = 0.6;

            GeneralSettingsButtonRightArrow.Visibility = Visibility.Visible;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            TransactionViewButtonRightArrow.Visibility = Visibility.Hidden;
            LineSummaryButtonRightArrow.Visibility = Visibility.Hidden;

            GeneralSettingsSection.Visibility = Visibility.Visible;
            ApplicationSettingsSection.Visibility = Visibility.Hidden;
            TransactionViewSection.Visibility = Visibility.Hidden;
            LineSummarySection.Visibility = Visibility.Hidden;

            //load transport bills data to table
            var bills = await _consoleHandler.getAllTransportBills();
            //MessageBox.Show(bills.Count.ToString());
            CustomerBillRowPanel.Children.Clear();
            foreach (TransportBillBlockModel bill in bills)
            {
                int idx = bills.IndexOf(bill) + 1;
                CustomerBillTableRow cr1 = new CustomerBillTableRow(idx.ToString(), bill.billNo, bill.lineName);
                CustomerBillRowPanel.Children.Add(cr1);
            }
        }

        private void ApplicationSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            ApplicationSettingsButton.Opacity = 1.0;
            GeneralSettingsButton.Opacity = 0.6;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Visible;
            GeneralSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            GeneralSettingsSection.Visibility = Visibility.Hidden;
            ApplicationSettingsSection.Visibility = Visibility.Visible;
            TransactionViewSection.Visibility = Visibility.Hidden;
            LineSummarySection.Visibility = Visibility.Hidden;
            TransactionViewButton.Opacity = 0.6;
            LineSummaryButton.Opacity = 0.6;
            TransactionViewButtonRightArrow.Visibility = Visibility.Hidden;
            LineSummaryButtonRightArrow.Visibility = Visibility.Hidden;
        }

        private async void TransactionViewButtonClick(object sender, RoutedEventArgs e)
        {
            // Update button states
            GeneralSettingsButton.Opacity = 0.6;
            ApplicationSettingsButton.Opacity = 0.6;
            TransactionViewButton.Opacity = 1.0;
            LineSummaryButton.Opacity = 0.6;

            GeneralSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            TransactionViewButtonRightArrow.Visibility = Visibility.Visible;
            LineSummaryButtonRightArrow.Visibility = Visibility.Hidden;

            // Show/hide sections
            GeneralSettingsSection.Visibility = Visibility.Hidden;
            ApplicationSettingsSection.Visibility = Visibility.Hidden;
            TransactionViewSection.Visibility = Visibility.Visible;
            LineSummarySection.Visibility = Visibility.Hidden;

            // Set default date to today
            TransactionDatePicker.SelectedDate = DateTime.Today;

            // Load line names into filter dropdown
            await LoadTransactionLineFilter();
        }

        private async void LineSummaryButtonClick(object sender, RoutedEventArgs e)
        {
            // Update button states
            GeneralSettingsButton.Opacity = 0.6;
            ApplicationSettingsButton.Opacity = 0.6;
            TransactionViewButton.Opacity = 0.6;
            LineSummaryButton.Opacity = 1.0;

            GeneralSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            ApplicationSettingsButtonRightArrow.Visibility = Visibility.Hidden;
            TransactionViewButtonRightArrow.Visibility = Visibility.Hidden;
            LineSummaryButtonRightArrow.Visibility = Visibility.Visible;

            // Show/hide sections
            GeneralSettingsSection.Visibility = Visibility.Hidden;
            ApplicationSettingsSection.Visibility = Visibility.Hidden;
            TransactionViewSection.Visibility = Visibility.Hidden;
            LineSummarySection.Visibility = Visibility.Visible;

            // Set default date to today
            LineSummaryDatePicker.SelectedDate = DateTime.Today;

            // Load line names into filter dropdown
            await LoadLineSummaryLineFilter();
        }

        private async Task LoadTransactionLineFilter()
        {
            try
            {
                var finalTransactionService = new FinalTransactionService(_consoleHandler.GetDbContext());
                // Get line names from both FinalTransaction and Transaction tables
                var lineNames = await finalTransactionService.GetAllDistinctLineNamesAsync();

                TransactionLineFilterCmb.Items.Clear();
                TransactionLineFilterCmb.Items.Add(new ComboBoxItem { Content = "All Lines", IsSelected = true });

                foreach (var lineName in lineNames)
                {
                    TransactionLineFilterCmb.Items.Add(new ComboBoxItem { Content = lineName });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading line filter: {ex.Message}");
            }
        }

        private async Task LoadLineSummaryLineFilter()
        {
            try
            {
                var finalTransactionService = new FinalTransactionService(_consoleHandler.GetDbContext());
                // Use GetAllDistinctLineNamesAsync to get line names from BOTH tables
                var lineNames = await finalTransactionService.GetAllDistinctLineNamesAsync();

                LineSummaryLineFilterCmb.Items.Clear();
                LineSummaryLineFilterCmb.Items.Add(new ComboBoxItem { Content = "All Lines", IsSelected = true });

                foreach (var lineName in lineNames)
                {
                    LineSummaryLineFilterCmb.Items.Add(new ComboBoxItem { Content = lineName });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading line filter: {ex.Message}");
            }
        }

        private async void TransactionSearchButton_Click2(object sender, RoutedEventArgs e)
        {
            try
            {
                var finalTransactionService = new FinalTransactionService(_consoleHandler.GetDbContext());

                var selectedLine = (TransactionLineFilterCmb.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var memberId = TransactionMemberIdFilterTxt.Text?.Trim();
                var selectedDate = TransactionDatePicker.SelectedDate?.ToString("yyyy-MM-dd");

                List<TransactionViewModel> transactions;

                // Get all combined transactions first
                transactions = await finalTransactionService.GetCombinedTransactionsAsync();

                // Apply line filter
                if (selectedLine != "All Lines" && !string.IsNullOrEmpty(selectedLine))
                {
                    transactions = transactions.Where(t => t.LineName == selectedLine).ToList();
                }

                // Apply member ID filter
                if (!string.IsNullOrEmpty(memberId))
                {
                    transactions = transactions.Where(t => t.MemberId != null && t.MemberId.Contains(memberId, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Apply date filter
                if (!string.IsNullOrEmpty(selectedDate))
                {
                    transactions = transactions.Where(t => t.Date == selectedDate).ToList();
                }

                // Clear existing rows
                TransactionTablePanel.Children.Clear();

                // Calculate totals
                int totalBoxes = 0, totalBags = 0, totalGold = 0, totalNormal = 0, totalWeight = 0;
                int totalWater = 0, totalMora = 0, totalThambi = 0, totalReject = 0;

                // Populate table
                foreach (var transaction in transactions)
                {
                    var row = CreateTransactionRow(transaction);
                    TransactionTablePanel.Children.Add(row);

                    totalBoxes += transaction.BoxCount;
                    totalBags += transaction.BagCount;
                    totalGold += transaction.GoldLeafWeight;
                    totalNormal += transaction.NormalLeafWeight;
                    totalWeight += transaction.TotalWeight;
                    totalWater += transaction.Water;
                    totalMora += transaction.Morapuwata;
                    totalThambi += transaction.Thambimata;
                    totalReject += transaction.Reject;
                }

                // Update summary
                TransactionTotalCount.Text = $"Total: {transactions.Count}";
                TransactionTotalBoxes.Text = totalBoxes.ToString();
                TransactionTotalBags.Text = totalBags.ToString();
                TransactionTotalGold.Text = totalGold.ToString();
                TransactionTotalNormal.Text = totalNormal.ToString();
                TransactionTotalWeight.Text = totalWeight.ToString();
                TransactionTotalWater.Text = totalWater.ToString();
                TransactionTotalMora.Text = totalMora.ToString();
                TransactionTotalThambi.Text = totalThambi.ToString();
                TransactionTotalReject.Text = totalReject.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching transactions: {ex.Message}");
                MessageBox.Show($"Error loading transactions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TransactionSearchButton_Click(object sender, RoutedEventArgs e)
        {
            //filtering parameters for table data
            var selectedLine = (TransactionLineFilterCmb.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var memberId = TransactionMemberIdFilterTxt.Text?.Trim();
            var selectedDate = TransactionDatePicker.SelectedDate?.ToString("yyyy-MM-dd");

            try
            {
                //fetch data from both tables
                List<FinalTransactionBlockModel> oldLineReportData = await _consoleHandler.printTransactionByDate_trans(selectedDate);
                //MessageBox.Show("number of rows found in "+oldLineReportData.Count());
                List<TransactionLogBlockModel> boxReportData = await _consoleHandler.printTransactionsBoxAndPendingBagsByDate_trans(selectedDate);
                //MessageBox.Show("number of rows found in " + boxReportData.Count());
                //merge data and finalize object list to populate the table
                List<FinalTransactionBlockModel> transactions = BlockModelConverter.ToFinalTransactionBlockModel(oldLineReportData, boxReportData);

                // Apply line filter
                if (selectedLine != "All Lines" && !string.IsNullOrEmpty(selectedLine))
                {
                    transactions = transactions.Where(t => t.linename == selectedLine).ToList();
                }

                // Apply member ID filter
                if (!string.IsNullOrEmpty(memberId))
                {
                    transactions = transactions.Where(t => t.barcode_details != null && t.barcode_details.Contains(memberId, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Apply date filter
                if (!string.IsNullOrEmpty(selectedDate))
                {
                    transactions = transactions.Where(t => t.date == selectedDate).ToList();
                }

                // Clear existing rows
                TransactionTablePanel.Children.Clear();

                // Initialize totals to zero
                int indexesTotal = 0, totalBagCount = 0, totalBoxCount = 0,
                    totalLeafWeight = 0, totalDalu = 0,
                    totalGoldLeafWeight = 0, totalGreenLeafWeight = 0,
                    totalWater = 0, totalMorapuwata = 0, totalThambimata = 0, totalReject = 0,
                    totalBagWeight = 0, totalBoxWeight = 0;

                // Populate table

                if (transactions.Any())
                {
                    foreach (var transaction in transactions)
                    {
                        // create and add row
                        var row = CreateCustomTransactionRow(transaction);
                        TransactionTablePanel.Children.Add(row);

                        //helper method to calculate box count
                        int tempBoxWeight = ((int)Math.Floor(transaction.real_value) - transaction.maximum_nomal_leaf_weight);
                        if (tempBoxWeight % 7 == 0) { totalBoxCount = (int)(tempBoxWeight / 3.5); }
                        else { totalBoxCount = tempBoxWeight / 4; }

                        //calculate total row values
                        totalBagCount += transaction.bag_count;
                        totalLeafWeight += transaction.total_leaf_weight + tempBoxWeight; //total weigt was fixed to include bag weight
                        totalGreenLeafWeight += transaction.actual_nomal_leaf_weight;
                        totalGoldLeafWeight += transaction.total_gold_leaf_weight;
                        totalWater += transaction.water;
                        totalMorapuwata += transaction.morapuwata;
                        totalThambimata += transaction.thambimata;
                        totalReject += transaction.reject;
                        totalBagWeight += transaction.bag_weight;
                        totalBoxWeight += tempBoxWeight;                    // Use the calculated box weight
                        //used as a temp variable for now
                        totalDalu += transaction.total_leaf_weight - (transaction.water + transaction.morapuwata
                                     + transaction.thambimata + transaction.reject + transaction.bag_weight);
                        int barcode_index = 0;
                        if (int.TryParse(transaction.barcode_details, out barcode_index))
                        {
                            indexesTotal += barcode_index;
                        }
                        else
                        {
                            indexesTotal += 0;
                        }
                    }

                    // assign total row values
                    TransactionTotalCount.Text = $"Total: {transactions.Count}";
                    TransactionTotalBoxes.Text = totalBoxCount.ToString();
                    TransactionTotalBags.Text = totalBagCount.ToString();
                    TransactionTotalNormal.Text = totalGreenLeafWeight.ToString();
                    TransactionTotalGold.Text = totalGoldLeafWeight.ToString();
                    TransactionTotalWeight.Text = totalLeafWeight.ToString();
                    TransactionTotalWater.Text = totalWater.ToString();
                    TransactionTotalMora.Text = totalMorapuwata.ToString();
                    TransactionTotalThambi.Text = totalThambimata.ToString();
                    TransactionTotalReject.Text = totalReject.ToString();
                }
                else
                {
                    //MessageBox.Show("list is empty to populate table");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Report error: " + ex.Message);
            }
        }

        private Border CreateTransactionRow(TransactionViewModel transaction)
        {
            // Set background color based on status
            var bgColor = transaction.Status == "Completed" ? "#E8F5E9" : "#FFF3E0"; // Green-ish for completed, orange-ish for queue

            var border = new Border
            {
                Height = 40,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 2, 0, 0)
            };

            // Column widths matching the XAML header: 75, 130, 90, 50, 50, 60, 65, 60, 55, 55, 60, 55, 85, 75
            var grid = new Grid { Margin = new Thickness(15, 0, 15, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(73) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            //grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });

            var memberId = new TextBlock { Text = transaction.MemberId ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(memberId, 0);
            grid.Children.Add(memberId);

            var name = new TextBlock { Text = transaction.Name ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(name, 1);
            grid.Children.Add(name);

            var line = new TextBlock { Text = transaction.LineName ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(line, 2);
            grid.Children.Add(line);

            var boxes = new TextBlock { Text = transaction.BoxCount.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(boxes, 3);
            grid.Children.Add(boxes);

            var bags = new TextBlock { Text = transaction.BagCount.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(bags, 4);
            grid.Children.Add(bags);

            var gold = new TextBlock { Text = transaction.GoldLeafWeight.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(gold, 5);
            grid.Children.Add(gold);

            var normal = new TextBlock { Text = transaction.NormalLeafWeight.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(normal, 6);
            grid.Children.Add(normal);

            var total = new TextBlock { Text = transaction.TotalWeight.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(total, 7);
            grid.Children.Add(total);

            var water = new TextBlock { Text = transaction.Water.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(water, 8);
            grid.Children.Add(water);

            var mora = new TextBlock { Text = transaction.Morapuwata.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(mora, 9);
            grid.Children.Add(mora);

            var thambi = new TextBlock { Text = transaction.Thambimata.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(thambi, 10);
            grid.Children.Add(thambi);

            var reject = new TextBlock { Text = transaction.Reject.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(reject, 11);
            grid.Children.Add(reject);

            //var date = new TextBlock { Text = transaction.Date ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            //Grid.SetColumn(date, 12);
            //grid.Children.Add(date);

            // Status tag with color coding
            var statusColor = transaction.Status == "Completed" ? "#2ECC71" : "#F39C12"; // Green for completed, Orange for queue
            var statusBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var status = new TextBlock { Text = transaction.Status, FontSize = 17, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold };
            statusBorder.Child = status;
            Grid.SetColumn(statusBorder, 13);
            grid.Children.Add(statusBorder);

            border.Child = grid;
            return border;
        }

        private Border CreateCustomTransactionRow(FinalTransactionBlockModel transaction)
        {
            //helper method to calculate box count
            int totalBoxCount = 0;
            int tempBoxWeight = ((int)Math.Floor(transaction.real_value) - transaction.maximum_nomal_leaf_weight);
            if (tempBoxWeight % 7 == 0) { totalBoxCount = (int)(tempBoxWeight / 3.5); }
            else { totalBoxCount = tempBoxWeight / 4; }

            // Set background color based on status
            // completed: bag count is not zero
            var bgColor = (transaction.bag_weight > 0) ? "#E8F5E9" : "#FFF3E0"; // Green-ish for completed, orange-ish for queue

            var border = new Border
            {
                Height = 40,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 2, 0, 0)
            };

            // Column widths matching the XAML header: 75, 130, 90, 50, 50, 60, 65, 60, 55, 55, 60, 55, 85, 75
            var grid = new Grid { Margin = new Thickness(15, 0, 15, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(73) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            //grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });

            var memberId = new TextBlock { Text = transaction.barcode_details ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(memberId, 0);
            grid.Children.Add(memberId);

            var name = new TextBlock { Text = transaction.name_with_initials ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(name, 1);
            grid.Children.Add(name);

            var line = new TextBlock { Text = transaction.linename ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(line, 2);
            grid.Children.Add(line);

            var boxes = new TextBlock { Text = totalBoxCount.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(boxes, 3);
            grid.Children.Add(boxes);

            var bags = new TextBlock { Text = transaction.bag_count.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(bags, 4);
            grid.Children.Add(bags);

            var gold = new TextBlock { Text = transaction.total_gold_leaf_weight.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(gold, 5);
            grid.Children.Add(gold);

            var normal = new TextBlock { Text = transaction.actual_nomal_leaf_weight.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(normal, 6);
            grid.Children.Add(normal);

            var total = new TextBlock { Text = transaction.total_leaf_weight.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(total, 7);
            grid.Children.Add(total);

            var water = new TextBlock { Text = transaction.water.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(water, 8);
            grid.Children.Add(water);

            var mora = new TextBlock { Text = transaction.morapuwata.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(mora, 9);
            grid.Children.Add(mora);

            var thambi = new TextBlock { Text = transaction.thambimata.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(thambi, 10);
            grid.Children.Add(thambi);

            var reject = new TextBlock { Text = transaction.reject.ToString(), FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(reject, 11);
            grid.Children.Add(reject);

            //var date = new TextBlock { Text = transaction.Date ?? "", FontSize = 17, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            //Grid.SetColumn(date, 12);
            //grid.Children.Add(date);

            // Status tag with color coding
            var statusColor = (transaction.bag_weight > 0) ? "#2ECC71" : "#F39C12"; // Green for completed, Orange for queue
            var statusBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var status = new TextBlock { Text = (transaction.bag_weight > 0) ? "Done" : "Pending", FontSize = 17, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold };
            statusBorder.Child = status;
            Grid.SetColumn(statusBorder, 13);
            grid.Children.Add(statusBorder);

            border.Child = grid;
            return border;
        }


        // fix this api call

        private async void LineSummarySearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedLine = (LineSummaryLineFilterCmb.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var selectedDate = LineSummaryDatePicker.SelectedDate?.ToString("yyyy-MM-dd");

                List<DailyReportRowBlockModel> dailyReportData = new List<DailyReportRowBlockModel>();

                //get all lines to fetch data seperately
                List<LineBlockModel> lines = null;
                lines = await _consoleHandler.getLineMasterData();
                int dailyId = 1;
                foreach (var lineMaster in lineMasterData)
                {
                    //get a single line with multiple rows
                    List<FinalTransactionBlockModel> oldLineReportData = await _consoleHandler.print_sta2_onCustomDate(lineMaster.LineName, selectedDate);
                    List<TransactionLogBlockModel> boxReportData = await _consoleHandler.GetBoxOnlyLineReportData(lineMaster.LineName, selectedDate);
                    List<FinalTransactionBlockModel> lineReportData =
                        BlockModelConverter.ToFinalTransactionBlockModel(oldLineReportData, boxReportData);


                    //create an obj per each line of the daily report
                    DailyReportRowBlockModel lineRow = new DailyReportRowBlockModel();
                    //assgin non-numeric vals using the first row before summing up
                    //currentMemberDetailsList_st2[0].linename;
                    lineRow.Id = dailyId;
                    lineRow.linename = lineMaster.LineName;
                    //MessageBox.Show(lineMaster.LineName);
                    //sum up only numbers through the loop to a single row
                    foreach (var tRow in lineReportData)
                    {
                        int totalBoxCount = 0;
                        int tempBoxWeight = ((int)Math.Floor(tRow.real_value) - tRow.maximum_nomal_leaf_weight);

                        if (tempBoxWeight % 7 == 0) { totalBoxCount = (int)(tempBoxWeight / 3.5); }
                        else { totalBoxCount = tempBoxWeight / 4; }

                        lineRow.box_count += totalBoxCount;
                        lineRow.bag_count += tRow.bag_count;
                        lineRow.maximum_nomal_leaf_weight += tRow.maximum_nomal_leaf_weight;
                        lineRow.total_leaf_weight += tRow.total_leaf_weight;
                        lineRow.actual_nomal_leaf_weight += tRow.actual_nomal_leaf_weight;
                        lineRow.total_gold_leaf_weight += tRow.total_gold_leaf_weight;
                        lineRow.water += tRow.water;
                        lineRow.morapuwata += tRow.morapuwata;
                        lineRow.thambimata += tRow.thambimata;
                        lineRow.reject += tRow.reject;
                        lineRow.bag_weight += tRow.bag_weight;
                        lineRow.box_weight += tempBoxWeight; // Use the calculated box weight
                        lineRow.final_green_leaf_count += tRow.final_green_leaf_count;
                        lineRow.final_gold_leaf_count += tRow.final_gold_leaf_count;
                        lineRow.real_value += tRow.real_value;
                    }
                    //add that row to the daily report row list
                    dailyReportData.Add(lineRow);

                    dailyId++;
                }
                dailyId = 1;

                // Clear existing rows
                LineSummaryTablePanel.Children.Clear();

                // Calculate totals
                int totalWeight = 0, totalBoxes = 0, totalBags = 0, totalGold = 0, totalNormal = 0, totalNet = 0;
                int totalWater = 0, totalMora = 0, totalThambi = 0, totalReject = 0;

                // Populate table
                foreach (var summary in dailyReportData)
                {
                    var row = CreateLineSummaryRow(summary);
                    LineSummaryTablePanel.Children.Add(row);

                    totalWeight += summary.total_leaf_weight + summary.box_weight;
                    totalBoxes += summary.box_count;
                    totalBags += summary.bag_count;
                    totalGold += summary.total_gold_leaf_weight;
                    totalNormal += summary.actual_nomal_leaf_weight;
                    totalNet += summary.total_leaf_weight - (summary.water + summary.morapuwata + summary.thambimata + summary.reject + summary.bag_weight);
                    totalWater += summary.water;
                    totalMora += summary.morapuwata;
                    totalThambi += summary.thambimata;
                    totalReject += summary.reject;
                }

                // Update summary
                //LineSummaryTotalLines.Text = $"Total Lines: {summaries.Count}";
                LineSummaryTotalMembers.Text = totalWeight.ToString();
                LineSummaryTotalBoxes.Text = totalBoxes.ToString();
                LineSummaryTotalBags.Text = totalBags.ToString();
                LineSummaryTotalGold.Text = totalGold.ToString();
                LineSummaryTotalNormal.Text = totalNormal.ToString();
                LineSummaryTotalWeight.Text = totalNet.ToString();
                LineSummaryTotalWater.Text = totalWater.ToString();
                LineSummaryTotalMora.Text = totalMora.ToString();
                LineSummaryTotalThambi.Text = totalThambi.ToString();
                LineSummaryTotalReject.Text = totalReject.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching line summary: {ex.Message}");
                MessageBox.Show($"Error loading line summary: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private Border CreateLineSummaryRow(DailyReportRowBlockModel summary)
        {
            var border = new Border
            {
                Height = 40,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 2, 0, 0)
            };

            // Column widths matching XAML: 140, 80, 70, 70, 90, 100, 100, 70, 70, 80, 70
            var grid = new Grid { Margin = new Thickness(10, 0, 10, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var lineName = new TextBlock { Text = summary.linename ?? "", FontSize = 18, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(lineName, 0);
            grid.Children.Add(lineName);

            var members = new TextBlock { Text = (summary.total_leaf_weight + summary.box_weight).ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(members, 1);
            grid.Children.Add(members);

            var boxes = new TextBlock { Text = summary.box_count.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(boxes, 2);
            grid.Children.Add(boxes);

            var bags = new TextBlock { Text = summary.bag_count.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(bags, 3);
            grid.Children.Add(bags);

            var gold = new TextBlock { Text = summary.total_gold_leaf_weight.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(gold, 4);
            grid.Children.Add(gold);

            var normal = new TextBlock { Text = summary.actual_nomal_leaf_weight.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(normal, 5);
            grid.Children.Add(normal);

            var total = new TextBlock { Text = (summary.total_leaf_weight - (summary.water + summary.morapuwata + summary.thambimata + summary.reject + summary.bag_weight)).ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(total, 6);
            grid.Children.Add(total);

            var water = new TextBlock { Text = summary.water.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(water, 7);
            grid.Children.Add(water);

            var mora = new TextBlock { Text = summary.morapuwata.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(mora, 8);
            grid.Children.Add(mora);

            var thambi = new TextBlock { Text = summary.thambimata.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(thambi, 9);
            grid.Children.Add(thambi);

            var reject = new TextBlock { Text = summary.reject.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(reject, 10);
            grid.Children.Add(reject);

            border.Child = grid;
            return border;
        }


        private async void LineSummarySearchButton_Click1(object sender, RoutedEventArgs e)
        {
            try
            {
                var finalTransactionService = new FinalTransactionService(_consoleHandler.GetDbContext());

                var selectedLine = (LineSummaryLineFilterCmb.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var selectedDate = LineSummaryDatePicker.SelectedDate?.ToString("yyyy-MM-dd");

                List<LineSummaryModel> summaries;

                // Apply filters based on line and date selection
                // Using Combined methods to get data from BOTH FinalTransaction and Transaction tables
                bool hasLineFilter = selectedLine != "All Lines" && !string.IsNullOrEmpty(selectedLine);
                bool hasDateFilter = !string.IsNullOrEmpty(selectedDate);

                if (hasLineFilter && hasDateFilter)
                {
                    summaries = await finalTransactionService.GetCombinedLineSummaryByLineNameAndDateAsync(selectedLine, selectedDate);
                }
                else if (hasLineFilter)
                {
                    summaries = await finalTransactionService.GetCombinedLineSummaryByLineNameAsync(selectedLine);
                }
                else if (hasDateFilter)
                {
                    summaries = await finalTransactionService.GetCombinedLineSummaryByDateAsync(selectedDate);
                }
                else
                {
                    summaries = await finalTransactionService.GetCombinedLineSummaryAsync();
                }

                // Clear existing rows
                LineSummaryTablePanel.Children.Clear();

                // Calculate totals
                int totalMembers = 0, totalBoxes = 0, totalBags = 0, totalGold = 0, totalNormal = 0, totalWeight = 0;
                int totalWater = 0, totalMora = 0, totalThambi = 0, totalReject = 0;

                // Populate table
                foreach (var summary in summaries)
                {
                    var row = CreateLineSummaryRow1(summary);
                    LineSummaryTablePanel.Children.Add(row);

                    totalMembers += summary.TotalMembers;
                    totalBoxes += summary.TotalBoxes;
                    totalBags += summary.TotalBags;
                    totalGold += summary.TotalGoldLeafWeight;
                    totalNormal += summary.TotalNormalLeafWeight;
                    totalWeight += summary.TotalWeight;
                    totalWater += summary.TotalWater;
                    totalMora += summary.TotalMorapuwata;
                    totalThambi += summary.TotalThambimata;
                    totalReject += summary.TotalReject;
                }

                // Update summary
                LineSummaryTotalLines.Text = $"Total Lines: {summaries.Count}";
                LineSummaryTotalMembers.Text = totalMembers.ToString();
                LineSummaryTotalBoxes.Text = totalBoxes.ToString();
                LineSummaryTotalBags.Text = totalBags.ToString();
                LineSummaryTotalGold.Text = totalGold.ToString();
                LineSummaryTotalNormal.Text = totalNormal.ToString();
                LineSummaryTotalWeight.Text = totalWeight.ToString();
                LineSummaryTotalWater.Text = totalWater.ToString();
                LineSummaryTotalMora.Text = totalMora.ToString();
                LineSummaryTotalThambi.Text = totalThambi.ToString();
                LineSummaryTotalReject.Text = totalReject.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching line summary: {ex.Message}");
                MessageBox.Show($"Error loading line summary: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateLineSummaryRow1(LineSummaryModel summary)
        {
            var border = new Border
            {
                Height = 40,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 2, 0, 0)
            };

            // Column widths matching XAML: 140, 80, 70, 70, 90, 100, 100, 70, 70, 80, 70
            var grid = new Grid { Margin = new Thickness(10, 0, 10, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            var lineName = new TextBlock { Text = summary.LineName ?? "", FontSize = 18, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Grid.SetColumn(lineName, 0);
            grid.Children.Add(lineName);

            var members = new TextBlock { Text = summary.TotalMembers.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(members, 1);
            grid.Children.Add(members);

            var boxes = new TextBlock { Text = summary.TotalBoxes.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(boxes, 2);
            grid.Children.Add(boxes);

            var bags = new TextBlock { Text = summary.TotalBags.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(bags, 3);
            grid.Children.Add(bags);

            var gold = new TextBlock { Text = summary.TotalGoldLeafWeight.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(gold, 4);
            grid.Children.Add(gold);

            var normal = new TextBlock { Text = summary.TotalNormalLeafWeight.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(normal, 5);
            grid.Children.Add(normal);

            var total = new TextBlock { Text = summary.TotalWeight.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(total, 6);
            grid.Children.Add(total);

            var water = new TextBlock { Text = summary.TotalWater.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(water, 7);
            grid.Children.Add(water);

            var mora = new TextBlock { Text = summary.TotalMorapuwata.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(mora, 8);
            grid.Children.Add(mora);

            var thambi = new TextBlock { Text = summary.TotalThambimata.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(thambi, 9);
            grid.Children.Add(thambi);

            var reject = new TextBlock { Text = summary.TotalReject.ToString(), FontSize = 18, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")) };
            Grid.SetColumn(reject, 10);
            grid.Children.Add(reject);

            border.Child = grid;
            return border;
        }

        // Auto-search when line filter changes in Transaction View
        private void TransactionLineFilterCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionViewSection.Visibility == Visibility.Visible)
            {
                TransactionSearchButton_Click(sender, new RoutedEventArgs());
            }
        }

        // Debounce timer for text input
        private DispatcherTimer? _transactionSearchTimer;

        // Auto-search when member ID text changes (with debounce)
        private void TransactionMemberIdFilterTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cancel previous timer if exists
            _transactionSearchTimer?.Stop();

            // Create new timer with 500ms delay
            _transactionSearchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _transactionSearchTimer.Tick += (s, args) =>
            {
                _transactionSearchTimer.Stop();
                if (TransactionViewSection.Visibility == Visibility.Visible)
                {
                    TransactionSearchButton_Click(sender, new RoutedEventArgs());
                }
            };
            _transactionSearchTimer.Start();
        }

        // Clear button for Transaction View
        private void TransactionClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset filters
            if (TransactionLineFilterCmb.Items.Count > 0)
            {
                TransactionLineFilterCmb.SelectedIndex = 0;
            }
            TransactionMemberIdFilterTxt.Text = "";
            TransactionDatePicker.SelectedDate = DateTime.Today;

            // Clear table
            TransactionTablePanel.Children.Clear();

            // Reset summary
            TransactionTotalCount.Text = "Total: 0";
            TransactionTotalBoxes.Text = "0";
            TransactionTotalBags.Text = "0";
            TransactionTotalGold.Text = "0";
            TransactionTotalNormal.Text = "0";
            TransactionTotalWeight.Text = "0";
            TransactionTotalWater.Text = "0";
            TransactionTotalMora.Text = "0";
            TransactionTotalThambi.Text = "0";
            TransactionTotalReject.Text = "0";
        }

        // Auto-search when date changes in Transaction View
        private void TransactionDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionViewSection.Visibility == Visibility.Visible)
            {
                TransactionSearchButton_Click(sender, new RoutedEventArgs());
            }
        }

        // Auto-search when line filter changes in Line Summary
        private void LineSummaryLineFilterCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LineSummarySection.Visibility == Visibility.Visible)
            {
                LineSummarySearchButton_Click(sender, new RoutedEventArgs());
            }
        }

        // Clear button for Line Summary
        private void LineSummaryClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset filter
            if (LineSummaryLineFilterCmb.Items.Count > 0)
            {
                LineSummaryLineFilterCmb.SelectedIndex = 0;
            }
            LineSummaryDatePicker.SelectedDate = DateTime.Today;

            // Clear table
            LineSummaryTablePanel.Children.Clear();

            // Reset summary
            LineSummaryTotalLines.Text = "Total Lines: 0";
            LineSummaryTotalMembers.Text = "0";
            LineSummaryTotalBoxes.Text = "0";
            LineSummaryTotalBags.Text = "0";
            LineSummaryTotalGold.Text = "0";
            LineSummaryTotalNormal.Text = "0";
            LineSummaryTotalWeight.Text = "0";
            LineSummaryTotalWater.Text = "0";
            LineSummaryTotalMora.Text = "0";
            LineSummaryTotalThambi.Text = "0";
            LineSummaryTotalReject.Text = "0";
        }

        // Auto-search when date changes in Line Summary
        private void LineSummaryDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LineSummarySection.Visibility == Visibility.Visible)
            {
                LineSummarySearchButton_Click(sender, new RoutedEventArgs());
            }
        }


        private void SelectBillDate_settings_Click(object sender, RoutedEventArgs e)
        {
            billDateCalendar.Visibility = Visibility.Visible;
            btnSelectDate.Visibility = Visibility.Collapsed;
        }

        private async void BillDateCalendar_settings_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (billDateCalendar.SelectedDate.HasValue)
            {
                string date = billDateCalendar.SelectedDate.Value.ToString("yyyy-MM-dd");
                txtBillDate_settings.Text = date;

                //load transport bills data to table
                var bills = await _consoleHandler.getAllTransportBills(date);
                
                
                CustomerBillRowPanel.Children.Clear();
                foreach (TransportBillBlockModel bill in bills)
                {
                    int idx = bills.IndexOf(bill) + 1;
                    CustomerBillTableRow cr1 = new CustomerBillTableRow(idx.ToString(), bill.billNo, bill.lineName);
                    CustomerBillRowPanel.Children.Add(cr1);
                }

                billDateCalendar.Visibility = Visibility.Collapsed;
                btnSelectDate.Visibility = Visibility.Visible;
            }
            billDateCalendar.Visibility = Visibility.Collapsed;
            btnSelectDate.Visibility = Visibility.Visible;
        }


        /*        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
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

                        }*/

        private async void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                statusLabel.Content = "Cloud syncing...";
                await _consoleHandler.cloudsync();
                statusLabel.Content = "Cloud sync completed";
            }
            catch
            {
                statusLabel.Content = "Cloud syncing failed...";
            }
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            // Repeat until no exception occursss
            bool firstItr = true; //to check the first iteration has passed(to show different status label content after first iteration)
            bool failed = false;
            do
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(1)..." : "Reverifying Database Status(1)...";
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
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(2)..." : "Reverifying Database Status(2)...";
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
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(3)..." : "Reverifying Database Status(3)...";
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

                //newly added api for members
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(4)..." : "Reverifying Database Status(4)...";
                    });
                    await _consoleHandler.verifyMembers();  // Uncomment when needed
                    //System.Diagnostics.Debug.WriteLine("================");
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(4)";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"Member(API_v2) Database Error: {ex.Message}";
                    });
                }

                //newly added api for lines
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(5)..." : "Reverifying Database Status(5)...";
                    });
                    await _consoleHandler.VerifyLines(Convert.ToInt32(_appConfig.branchId));  //coop:1 || coop-cola:2 || allan-valley:3  //BRANCHCHANGE
                    //System.Diagnostics.Debug.WriteLine("================");
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(5)";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"Lines(API_v2) Database Error: {ex.Message}";
                    });
                }
                //VerifyLines()


                //await _consoleHandler.verifyMembers();
                //System.Diagnostics.Debug.WriteLine("================");

                // Transaction verification is commented out, include it when needed
                try
                {
                    bool _isoks = await _consoleHandler.verifyTransactionsCloudCheck();
                    failed = !_isoks;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"Transaction Error: {ex.Message}";
                    });
                }

            } while (failed);
            //resetting value for next execution 
            firstItr = true;

            ////restart the application
            //// Get the current application's executable path
            //string exePath = Process.GetCurrentProcess().MainModule.FileName;
            //// Start a new instance of the application
            //Process.Start(exePath);
            //// Shutdown the current application
            //Application.Current.Shutdown();

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

        private void BarcodeTxt_st1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the input text is numeric
            e.Handled = !IsTextNumeric(e.Text);
            // Check whether a line name is selected to get-line wise data
            if (lineNameCmb_st1.SelectedItem == null)
            {
                MessageBox.Show("ප්‍රවාහන මාර්ගය පලමුව තෝරාගන්න");
                e.Handled = true;
                //return;
            }
        }

        private void BarcodeTxt_st2_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Check if the input text is numeric
            e.Handled = !IsTextNumeric(e.Text);
            // Check whether a line name is selected to get-line wise data
            if (lineNameCmb_st2.SelectedItem == null)
            {
                MessageBox.Show("ප්‍රවාහන මාර්ගය පලමුව තෝරාගන්න");
                e.Handled = true;
                //return;
            }
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
        /*
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

        */

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
            AdminFrame.Visibility = Visibility.Collapsed;
            SettingsFrame.Visibility = Visibility.Collapsed;
            StationMainFrame.Visibility = Visibility.Collapsed;
            IntroFrame.Visibility = Visibility.Visible;

            bool internetAvailable = false;
            bool failed = false;

            // Check internet connection
            //try
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        statusLabel.Content = "Checking Internet Connection...";
            //    });

            //    internetAvailable = await Task.Run(() =>
            //    {
            //        try
            //        {
            //            using (var client = new System.Net.WebClient())
            //            using (client.OpenRead("https://www.google.com"))
            //                return true;
            //        }
            //        catch
            //        {
            //            return false;
            //        }
            //    });

            //    if (!internetAvailable)
            //    {
            //        Dispatcher.Invoke(() =>
            //        {
            //            statusLabel.Content = "No internet connection. Startup aborted.";
            //        });

            //        IntroFrame.Visibility = Visibility.Collapsed;
            //        LoginFrame.Visibility = Visibility.Visible;

            //        runtimeService.StartFileWatcher();
            //        runtimeService.StartTimer();

            //        return;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        statusLabel.Content = $"Error checking internet: {ex.Message}";
            //    });
            //    return;
            //}

            // Repeat until no exception occursss
            /*bool firstItr = true; //to check the first iteration has passed(to show different status label content after first iteration)
            do
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(1)..." : "Reverifying Database Status(1)...";
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
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(2)..." : "Reverifying Database Status(2)...";
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
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(3)..." : "Reverifying Database Status(3)...";
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

                //newly added api for members
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(4)..." : "Reverifying Database Status(4)...";
                    });
                    await _consoleHandler.verifyMembers();  // Uncomment when needed
                    //System.Diagnostics.Debug.WriteLine("================");
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(4)";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"Member(API_v2) Database Error: {ex.Message}";
                    });
                }

                //newly added api for lines
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = (firstItr) ? "Verifying Database Status(5)..." : "Reverifying Database Status(5)...";
                    });
                    await _consoleHandler.VerifyLines();  // Uncomment when needed
                    //System.Diagnostics.Debug.WriteLine("================");
                    Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = "DB Verified(5)";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        failed = true;
                        statusLabel.Content = $"Lines(API_v2) Database Error: {ex.Message}";
                    });
                }
                //VerifyLines()


                //await _consoleHandler.verifyMembers();
                //System.Diagnostics.Debug.WriteLine("================");

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
            //resetting value for next execution 
            firstItr = true;
*/
            IntroFrame.Visibility = Visibility.Collapsed;
            LoginFrame.Visibility = Visibility.Visible;

            runtimeService.StartFileWatcher();
            runtimeService.StartTimer();
        }

        //__________adminFrame-----------------------------------------------------------------
        //______Report Frame____________________________________________________________________
        private void ReportCalender_admin_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReportCalender_admin.SelectedDate.HasValue)
            {
                // Format as "yyyy-MM-dd" and assign to the TextBlock
                reportDateTxt_admin.Text = ReportCalender_admin.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                reportDateTxt_admin.Text = reportDate;
            }
        }



        //_______Reports_______________________________________________________________________



        //________________Line Report
        //private async void printQuick1LineReportBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (lineNameCmb_st2.SelectedItem == null)
        //    {
        //        MessageBox.Show("ප්‍රවා හන මා ර්ගය ඇතුලත් කරන්න");
        //        return;
        //    }

        //    try
        //    {
        //        List<FinalTransactionBlockModel> lineReportData = await _consoleHandler.print_sta2(lineNameCmb_st2.SelectedItem.ToString());
        //        foreach (var transaction in lineReportData)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
        //        }


        //        PrintDialog printDialog = new PrintDialog();
        //        if (printDialog.ShowDialog() == true)
        //        {
        //            DrawingVisual visual = new DrawingVisual();
        //            using (DrawingContext dc = visual.RenderOpen())
        //            {
        //                DrawLineReportPage(dc, lineReportData, lineNameCmb_st2.SelectedItem.ToString());
        //            }
        //            printDialog.PrintVisual(visual, "Print Document");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Report data fetching error: " + ex.Message);
        //    }
        //}


        //based on current date
        private async void printQuickLineReportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (lineNameCmb_st2.SelectedItem == null)
            {
                MessageBox.Show("ප්‍රවා හන මා ර්ගය ඇතුලත් කරන්න");
                return;
            }
            //get the selected date TODO: get today's date(mmm i can get from globals)

            string dateNow = DateTime.Now.ToString("yyyy-MM-dd");
            // Now reportDate holds e.g. "2025-06-15".


            string lineName = lineNameCmb_st2.SelectedItem.ToString();
            try
            {
                List<FinalTransactionBlockModel> oldLineReportData = await _consoleHandler.print_sta2_onCustomDate(lineName, dateNow);
                if (!oldLineReportData.Any())
                {
                    //MessageBox.Show("st2 list is empty"); 
                }
                List<TransactionLogBlockModel> boxReportData = await _consoleHandler.GetBoxOnlyLineReportData(lineName, reportDate);
                if (!boxReportData.Any())
                {
                    //MessageBox.Show("st1 box only list is empty"); 
                }
                List<FinalTransactionBlockModel> lineReportData =
                    BlockModelConverter.ToFinalTransactionBlockModel(oldLineReportData, boxReportData);




                /*foreach (var transaction in lineReportData)
                {
                   System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
                }*/
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Initialize totals to zero
                    int totalBagCount = 0, totalBoxCount = 0, totalLeafWeight = 0;
                    int totalWater = 0, totalMorapuwata = 0, totalThambimata = 0, indexesTotal = 0,
                        totalReject = 0, totalBagWeight = 0, totalBoxWeight = 0, totalDalu = 0;
                    // Only calculate totals if there's data
                    if (lineReportData.Any())
                    {
                        foreach (var transaction in lineReportData)
                        {
                            //calculate box count
                            int tempBoxWeight = ((int)Math.Floor(transaction.real_value) - transaction.maximum_nomal_leaf_weight);
                            if (tempBoxWeight % 7 == 0) { totalBoxCount = (int)(tempBoxWeight / 3.5); }
                            else { totalBoxCount = tempBoxWeight / 4; }

                            //(int)Math.Floor(scalerWeight)
                            //totalBoxCount += ((int)Math.Floor(transaction.real_value) - transaction.maximum_nomal_leaf_weight) / 4; //wrong boxFix
                            //MessageBox.Show(totalBoxCount + "=" + (int)Math.Floor(transaction.real_value) + "-" + transaction.maximum_nomal_leaf_weight +"/4");
                            //MessageBox.Show(totalBagCount + "=" + (int)Math.Floor(transaction.real_value) + "-" + transaction.maximum_nomal_leaf_weight);
                            totalBagCount += transaction.bag_count;
                            totalLeafWeight += transaction.total_leaf_weight + tempBoxWeight; //total weigt was fixed to include bag weight
                            totalWater += transaction.water;
                            totalMorapuwata += transaction.morapuwata;
                            totalThambimata += transaction.thambimata;
                            totalReject += transaction.reject;
                            totalBagWeight += transaction.bag_weight;
                            totalBoxWeight += tempBoxWeight;                    // Use the calculated box weight
                            totalDalu += transaction.total_leaf_weight - (transaction.water + transaction.morapuwata
                                         + transaction.thambimata + transaction.reject + transaction.bag_weight);
                            int barcode_index = 0;
                            if (int.TryParse(transaction.barcode_details, out barcode_index))
                            {
                                indexesTotal += barcode_index;
                            }
                            else
                            {
                                indexesTotal += 0;
                            }
                        }
                    }
                    else
                    {
                        //MessageBox.Show("list is empty"); 
                    }

                    // Pagination setup - ensure at least 1 page even for empty data
                    int pageSize = 30;
                    int totalPages = lineReportData.Count == 0 ? 1 : (int)Math.Ceiling((double)lineReportData.Count / pageSize);
                    FixedDocument fixedDoc = new FixedDocument();
                    fixedDoc.DocumentPaginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                    for (int page = 0; page < totalPages; page++)
                    {
                        var pageData = lineReportData.Count == 0
                            ? new List<FinalTransactionBlockModel>()  // Empty page
                            : lineReportData
                                .Skip(page * pageSize)
                                .Take(pageSize)
                                .ToList();

                        FixedPage fixedPage = new FixedPage();
                        Canvas canvas = CreateLineReportPage(
                            basedDate: dateNow.ToString(),
                            pageData: pageData,
                            pageNumber: page + 1,
                            totalPages: totalPages,
                            isLastPage: page == totalPages - 1,
                            totals: new TotalRow(
                                indexesTotal,
                                totalBagCount, totalBoxCount, totalLeafWeight,
                                totalWater, totalMorapuwata, totalThambimata,
                                totalReject, totalBagWeight, totalBoxWeight, totalDalu
                            ), lineName
                        );

                        fixedPage.Children.Add(canvas);
                        PageContent pageContent = new PageContent();
                        pageContent.Child = fixedPage;
                        fixedDoc.Pages.Add(pageContent);
                    }

                    printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Multi-Page Report");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Report error: " + ex.Message);
            }
        }




        //based on a given report date
        private async void printLineReportBtn_Click(object sender, RoutedEventArgs e)
        {

            if (lineNameCmb_admin.SelectedItem == null)
            {
                MessageBox.Show("ප්‍රවා හන මා ර්ගය ඇතුලත් කරන්න");
                return;
            }
            //get the selected date
            if (ReportCalender_admin.SelectedDate.HasValue)
            {
                reportDate = ReportCalender_admin.SelectedDate.Value.ToString("yyyy-MM-dd");
                // Now reportDate holds e.g. "2025-06-15". default is today's date in default as initialized in the global variable
            }
            else
            {
                //replacing the date with today's date as default(already handled in the global variable)
                //MessageBox.Show("දිනයක් තෝරාගන්න");
                //return;
            }
            string lineName = lineNameCmb_admin.SelectedItem.ToString();
            try
            {
                List<FinalTransactionBlockModel> oldLineReportData = await _consoleHandler.print_sta2_onCustomDate(lineName, reportDate);
                if (!oldLineReportData.Any())
                {
                    //MessageBox.Show("st2 list is empty"); 
                }
                List<TransactionLogBlockModel> boxReportData = await _consoleHandler.GetBoxOnlyLineReportData(lineName, reportDate);
                if (!boxReportData.Any())
                {
                    //MessageBox.Show("st1 box only list is empty"); 
                }
                List<FinalTransactionBlockModel> lineReportData =
                    BlockModelConverter.ToFinalTransactionBlockModel(oldLineReportData, boxReportData);




                /*foreach (var transaction in lineReportData)
                {
                   System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
                }*/
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Initialize totals to zero
                    int totalBagCount = 0, totalBoxCount = 0, totalLeafWeight = 0, indexesTotal = 0;
                    int totalWater = 0, totalMorapuwata = 0, totalThambimata = 0,
                        totalReject = 0, totalBagWeight = 0, totalBoxWeight = 0, totalDalu = 0;
                    // Only calculate totals if there's data
                    if (lineReportData.Any())
                    {
                        foreach (var transaction in lineReportData)
                        {
                            //calculate box count
                            int tempBoxWeight = ((int)Math.Floor(transaction.real_value) - transaction.maximum_nomal_leaf_weight);
                            if (tempBoxWeight % 7 == 0) { totalBoxCount = (int)(tempBoxWeight / 3.5); }
                            else { totalBoxCount = tempBoxWeight / 4; }

                            //(int)Math.Floor(scalerWeight)
                            //totalBoxCount += ((int)Math.Floor(transaction.real_value) - transaction.maximum_nomal_leaf_weight) / 4; //wrong boxFix
                            //MessageBox.Show(totalBoxCount + "=" + (int)Math.Floor(transaction.real_value) + "-" + transaction.maximum_nomal_leaf_weight +"/4");
                            //MessageBox.Show(totalBagCount + "=" + (int)Math.Floor(transaction.real_value) + "-" + transaction.maximum_nomal_leaf_weight);
                            totalBagCount += transaction.bag_count;
                            totalLeafWeight += transaction.total_leaf_weight + tempBoxWeight; //total weigt was fixed to include bag weight
                            totalWater += transaction.water;
                            totalMorapuwata += transaction.morapuwata;
                            totalThambimata += transaction.thambimata;
                            totalReject += transaction.reject;
                            totalBagWeight += transaction.bag_weight;
                            totalBoxWeight += tempBoxWeight;                    // Use the calculated box weight
                            totalDalu += transaction.total_leaf_weight - (transaction.water + transaction.morapuwata
                                         + transaction.thambimata + transaction.reject + transaction.bag_weight);
                            int barcode_index = 0;
                            if (int.TryParse(transaction.barcode_details, out barcode_index))
                            {
                                indexesTotal += barcode_index;
                            }
                            else
                            {
                                indexesTotal += 0;
                            }
                        }
                    }
                    else
                    {
                        //MessageBox.Show("list is empty"); 
                    }

                    // Pagination setup - ensure at least 1 page even for empty data
                    int pageSize = 30;
                    int totalPages = lineReportData.Count == 0 ? 1 : (int)Math.Ceiling((double)lineReportData.Count / pageSize);
                    FixedDocument fixedDoc = new FixedDocument();
                    fixedDoc.DocumentPaginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);


                    for (int page = 0; page < totalPages; page++)
                    {
                        var pageData = lineReportData.Count == 0
                            ? new List<FinalTransactionBlockModel>()  // Empty page
                            : lineReportData
                                .Skip(page * pageSize)
                                .Take(pageSize)
                                .ToList();

                        FixedPage fixedPage = new FixedPage();
                        Canvas canvas = CreateLineReportPage(
                            basedDate: reportDate.ToString(),
                            pageData: pageData,
                            pageNumber: page + 1,
                            totalPages: totalPages,
                            isLastPage: page == totalPages - 1,
                            totals: new TotalRow(
                                indexesTotal,
                                totalBagCount, totalBoxCount, totalLeafWeight,
                                totalWater, totalMorapuwata, totalThambimata,
                                totalReject, totalBagWeight, totalBoxWeight, totalDalu
                            ), lineName
                        );

                        fixedPage.Children.Add(canvas);
                        PageContent pageContent = new PageContent();
                        pageContent.Child = fixedPage;
                        fixedDoc.Pages.Add(pageContent);
                    }

                    printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Multi-Page Report");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Report error: " + ex.Message);
            }
        }

        private Canvas CreateLineReportPage(string basedDate, List<FinalTransactionBlockModel> pageData, int pageNumber,
            int totalPages, bool isLastPage, TotalRow totals, string lineName)
        {
            Canvas canvas = new Canvas { Width = 816, Height = 1056 }; // Standard letter size
            double yPos = 50;
            Typeface typeface = new Typeface("Arial");
            double fontSize = 13;
            double headerFontSize = 10;
            Brush brush = Brushes.Black;

            // Add main headers
            AddText(canvas, "සීමාසහිත මොරවක්කොරළේ තේ නිපදවන්නන්ගේ සමුපකාර සමිතිය", 14, 816 / 2, yPos, true);
            yPos += 30;
            AddText(canvas, _appConfig.branchName, 14, 816 / 2, yPos, true); //මොරවක්කෝරලේ තේ කම්හල || නව ඇලන්වැලි තේ කම්හල || කෝප්කෝලා තේ කම්හල //BRANCHCHANGE
            yPos += 30;
            AddText(canvas, "ප්‍රවාහන මාර්ග වාර්තාව - " + lineName + "(" + basedDate + ")", 14, 816 / 2, yPos, true);
            yPos += 30;

            // Report metadata
            AddText(canvas, $"Report Issue Index: IDX-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                fontSize, 50, yPos);
            yPos += 20;
            AddText(canvas, $"Date Issued: {DateTime.Now:yyyy.MM.dd}", fontSize, 50, yPos);
            yPos += 40;

            // Table header
            string[] headers = { "අං", "සාමාජික අං", "ගෝනි(n)", "පෙට්ටි(n)", "මුළු බර", "වතුරට", "මෝරපුවට", "තැමිණීමට", "ප්‍රතික්ෂේපිත", "ගෝනි(KG)", "පෙට්ටි(KG)", "දළු(KG)" };
            double[] headerPositions = { 46, 100, 220, 270, 315, 360, 430, 490, 570, 650, 710, 760 };

            // Draw header background
            AddRectangle(canvas, 40, yPos - 5, 816 - 80, 25, Brushes.White);
            for (int i = 0; i < headers.Length; i++)
            {
                bool isNumericColumn = i >= 2; // Columns from index 2 are numeric
                AddText(canvas, headers[i], headerFontSize, headerPositions[i], yPos, rightAlign: isNumericColumn, brush: Brushes.Black);
            }
            yPos += 25;

            // Data rows
            bool isAlternate = false;
            foreach (var transaction in pageData)
            {
                //if (isAlternate)
                //{
                //    AddRectangle(canvas, 40, yPos - 2, 816 - 80, 20, Brushes.LightGray);
                //}
                AddRectangle(canvas, 40, yPos - 2, 816 - 80, 20, Brushes.White);
                AddTransactionRow(canvas, transaction, headerPositions, yPos, fontSize);
                yPos += 20;
                isAlternate = !isAlternate; // Alternate row color
            }
            AddSignatureLine(canvas, "Authorised by (Supervisor)", 50, yPos + 120);
            AddSignatureLine(canvas, "Authorised by (Leaf Weighting Officer)", 315, yPos + 120);
            AddSignatureLine(canvas, "Authorised by (Bag Weighting Officer)", 570, yPos + 120);
            // Totals and signatures (last page only)
            if (isLastPage)
            {
                // Totals row { 50, 100, 220, 270, 315, 360, 430, 490, 570, 650, 710, 760 };
                AddRectangle(canvas, 40, yPos - 2, 816 - 80, 20, Brushes.White);
                //newly added index sum to totals
                AddText(canvas, totals.indexSum.ToString(), fontSize, 130, yPos, rightAlign: false);
                AddText(canvas, totals.BagCount.ToString(), fontSize, 220, yPos, rightAlign: true);
                AddText(canvas, totals.BoxCount.ToString(), fontSize, 270, yPos, rightAlign: true);
                AddText(canvas, totals.LeafWeight.ToString(), fontSize, 315, yPos, rightAlign: true);
                AddText(canvas, totals.Water.ToString(), fontSize, 360, yPos, rightAlign: true);
                AddText(canvas, totals.Morapuwata.ToString(), fontSize, 430, yPos, rightAlign: true);
                AddText(canvas, totals.Thambimata.ToString(), fontSize, 490, yPos, rightAlign: true);
                AddText(canvas, totals.Reject.ToString(), fontSize, 570, yPos, rightAlign: true);
                AddText(canvas, totals.BagWeight.ToString(), fontSize, 650, yPos, rightAlign: true);
                AddText(canvas, totals.BoxWeight.ToString(), fontSize, 710, yPos, rightAlign: true);
                AddText(canvas, totals.Dalu.ToString(), fontSize, 760, yPos, rightAlign: true);

                // Signatures
                //AddSignatureLine(canvas, "Authorised by (Supervisor)", 50, yPos + 60);
                //AddSignatureLine(canvas, "Authorised by (Leaf Weighting Officer)", 816 - 350, yPos + 60);
            }

            // Page number
            AddText(canvas, $"Page {pageNumber} of {totalPages}", fontSize, 700, 1000);

            return canvas;
        }

        // Helper methods
        private void AddText(Canvas canvas, string text, double fontSize, double x, double y,
    bool centerX = false, Brush brush = null, bool rightAlign = false)
        {
            var txt = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Arial"),
                FontSize = fontSize,
                Foreground = brush ?? Brushes.Black
            };

            txt.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (centerX)
            {
                Canvas.SetLeft(txt, x - txt.DesiredSize.Width / 2);
            }
            else if (rightAlign)
            {
                Canvas.SetLeft(txt, x - txt.DesiredSize.Width);
            }
            else
            {
                Canvas.SetLeft(txt, x);
            }

            Canvas.SetTop(txt, y);
            canvas.Children.Add(txt);
        }

        //private void AddRectangle(Canvas canvas, double x, double y, double width,
        //    double height, Brush fill)
        //{
        //    var rect = new Rectangle
        //    {
        //        Width = width,
        //        Height = height,
        //        Fill = fill,
        //        Stroke = Brushes.Black, // Sets the border color to black
        //        StrokeThickness = 1     // Sets the border thickness (adjust as needed)
        //    };
        //    Canvas.SetLeft(rect, x);
        //    Canvas.SetTop(rect, y);
        //    canvas.Children.Add(rect);
        //}
        private void AddRectangle(Canvas canvas, double x, double y, double width, double height, Brush fill)
        {
            var rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = fill
            };

            var border = new Border
            {
                BorderBrush = Brushes.Black, // Black border
                BorderThickness = new Thickness(1, 1, 1, 1), // Only bottom border (left, top, right, bottom)
                Child = rect
            };

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            canvas.Children.Add(border);
        }

        private void AddTransactionRow(Canvas canvas, FinalTransactionBlockModel transaction,
            double[] positions, double y, double fontSize)
        {
            //calculate box weight and count 
            int totalBoxCount = 0;
            int tempBoxWeight = ((int)Math.Floor(transaction.real_value) - transaction.maximum_nomal_leaf_weight);
            if (tempBoxWeight % 7 == 0) { totalBoxCount = (int)(tempBoxWeight / 3.5); }
            else { totalBoxCount = tempBoxWeight / 4; }
            // Left-aligned columns
            AddText(canvas, transaction.Id.ToString(), fontSize, positions[0], y);
            AddText(canvas, transaction.barcode_details ?? "", fontSize, positions[1], y);

            // Right-aligned numeric columns
            AddText(canvas, transaction.bag_count.ToString(), fontSize, positions[2], y, rightAlign: true);
            AddText(canvas, totalBoxCount.ToString(), fontSize, positions[3], y, rightAlign: true); // Box count
            AddText(canvas, (transaction.total_leaf_weight + tempBoxWeight).ToString(), fontSize, positions[4], y, rightAlign: true); //total weigt was fixed to include bag weight
            AddText(canvas, transaction.water.ToString(), fontSize, positions[5], y, rightAlign: true);
            AddText(canvas, transaction.morapuwata.ToString(), fontSize, positions[6], y, rightAlign: true);
            AddText(canvas, transaction.thambimata.ToString(), fontSize, positions[7], y, rightAlign: true);
            AddText(canvas, transaction.reject.ToString(), fontSize, positions[8], y, rightAlign: true);
            AddText(canvas, transaction.bag_weight.ToString(), fontSize, positions[9], y, rightAlign: true);
            AddText(canvas, tempBoxWeight.ToString(), fontSize, positions[10], y, rightAlign: true); //recently added box weight
            AddText(canvas, (transaction.total_leaf_weight - (transaction.water +
                transaction.morapuwata + transaction.thambimata +
                transaction.reject + transaction.bag_weight)).ToString(),
                fontSize, positions[11], y, rightAlign: true);
        }

        private void AddDailyTransactionRow(Canvas canvas, DailyReportRowBlockModel transaction,
            double[] positions, double y, double fontSize)
        {
            // Left-aligned columns
            AddText(canvas, transaction.Id.ToString(), fontSize, positions[0], y);
            AddText(canvas, transaction.linename ?? "", fontSize, positions[1], y);

            // Right-aligned numeric columns
            AddText(canvas, transaction.bag_count.ToString(), fontSize, positions[2], y, rightAlign: true);
            AddText(canvas, transaction.box_count.ToString(), fontSize, positions[3], y, rightAlign: true); // boxFix: Box count fix added
            AddText(canvas, (transaction.total_leaf_weight + transaction.box_weight).ToString(), fontSize, positions[4], y, rightAlign: true); //total weigt was fixed to include bag weight   
            AddText(canvas, transaction.water.ToString(), fontSize, positions[5], y, rightAlign: true);
            AddText(canvas, transaction.morapuwata.ToString(), fontSize, positions[6], y, rightAlign: true);
            AddText(canvas, transaction.thambimata.ToString(), fontSize, positions[7], y, rightAlign: true);
            AddText(canvas, transaction.reject.ToString(), fontSize, positions[8], y, rightAlign: true);
            AddText(canvas, transaction.bag_weight.ToString(), fontSize, positions[9], y, rightAlign: true);
            AddText(canvas, transaction.box_weight.ToString(), fontSize, positions[10], y, rightAlign: true);
            AddText(canvas, (transaction.total_leaf_weight - (transaction.water +
                transaction.morapuwata + transaction.thambimata +
                transaction.reject + transaction.bag_weight)).ToString(),
                fontSize, positions[11], y, rightAlign: true);
        }

        private void AddSignatureLine(Canvas canvas, string label, double x, double y)
        {
            var line = new Line
            {
                X1 = x,
                X2 = x + 220,
                Y1 = y,
                Y2 = y,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            canvas.Children.Add(line);
            AddText(canvas, label, 10, x, y + 5);
        }

        // Helper record for totals
        private class TotalRow
        {
            public int indexSum { get; }
            public int BagCount { get; }
            public int BoxCount { get; }
            public int LeafWeight { get; }
            public int Water { get; }
            public int Morapuwata { get; }
            public int Thambimata { get; }
            public int Reject { get; }
            public int BagWeight { get; }
            public int BoxWeight { get; }
            public int Dalu { get; }

            public TotalRow(int indexSum, int bagCount, int boxCount, int leafWeight, int water,
                           int morapuwata, int thambimata, int reject, int bagWeight, int boxWeight, int dalu)
            {
                this.indexSum = indexSum;
                BagCount = bagCount;
                BoxCount = boxCount;
                LeafWeight = leafWeight;
                Water = water;
                Morapuwata = morapuwata;
                Thambimata = thambimata;
                Reject = reject;
                BagWeight = bagWeight;
                BoxWeight = boxWeight;
                Dalu = dalu;
            }
        }



        //_________________Daily Report___________________________________
        private async void printDailyReportBtn_Click(object sender, RoutedEventArgs e)
        {
            //get the selected date
            if (ReportCalender_admin.SelectedDate.HasValue)
            {
                reportDate = ReportCalender_admin.SelectedDate.Value.ToString("yyyy-MM-dd");
                // Now reportDate holds e.g. "2025-06-15". default is today's date in defalut as initialized in the global variable
            }
            else
            {
                //replacing the date with today's date as default(already handled in the global variable)
                //MessageBox.Show("දිනයක් තෝරාගන්න");
                //return;
            }

            List<DailyReportRowBlockModel> dailyReportData = new List<DailyReportRowBlockModel>();

            //get all lines to fetch data seperately
            List<LineBlockModel> lines = null;
            lines = await _consoleHandler.getLineMasterData();
            int dailyId = 1;
            foreach (var lineMaster in lineMasterData)
            {
                //get a single line with multiple rows
                List<FinalTransactionBlockModel> oldLineReportData = await _consoleHandler.print_sta2_onCustomDate(lineMaster.LineName, reportDate);
                if (!oldLineReportData.Any())
                {
                    //MessageBox.Show("st2 list is empty"); 
                }
                List<TransactionLogBlockModel> boxReportData = await _consoleHandler.GetBoxOnlyLineReportData(lineMaster.LineName, reportDate);
                if (!boxReportData.Any())
                {
                    //MessageBox.Show("st1 box only list is empty"); 
                }
                List<FinalTransactionBlockModel> lineReportData =
                    BlockModelConverter.ToFinalTransactionBlockModel(oldLineReportData, boxReportData);

                //create an obj per each line of the daily report
                DailyReportRowBlockModel lineRow = new DailyReportRowBlockModel();
                //assgin non-numeric vals using the first row before summing up
                //currentMemberDetailsList_st2[0].linename;
                lineRow.Id = dailyId;
                lineRow.linename = lineMaster.LineName;
                //MessageBox.Show(lineMaster.LineName);
                //sum up only numbers through the loop to a single row
                foreach (var tRow in lineReportData)
                {
                    int totalBoxCount = 0;
                    int tempBoxWeight = ((int)Math.Floor(tRow.real_value) - tRow.maximum_nomal_leaf_weight);

                    if (tempBoxWeight % 7 == 0) { totalBoxCount = (int)(tempBoxWeight / 3.5); }
                    else { totalBoxCount = tempBoxWeight / 4; }

                    //lineRow.box_count += ((int)Math.Floor(tRow.real_value) - tRow.maximum_nomal_leaf_weight) / 4; //wrong boxFix
                    lineRow.box_count += totalBoxCount;
                    lineRow.bag_count += tRow.bag_count;
                    lineRow.maximum_nomal_leaf_weight = tRow.maximum_nomal_leaf_weight;
                    lineRow.total_leaf_weight += tRow.total_leaf_weight;
                    lineRow.actual_nomal_leaf_weight += tRow.actual_nomal_leaf_weight;
                    lineRow.total_gold_leaf_weight += tRow.total_gold_leaf_weight;
                    lineRow.water += tRow.water;
                    lineRow.morapuwata += tRow.morapuwata;
                    lineRow.thambimata += tRow.thambimata;
                    lineRow.reject += tRow.reject;
                    lineRow.bag_weight += tRow.bag_weight;
                    lineRow.box_weight += tempBoxWeight; // Use the calculated box weight
                    lineRow.final_green_leaf_count += tRow.final_green_leaf_count;
                    lineRow.final_gold_leaf_count += tRow.final_gold_leaf_count;
                    lineRow.real_value += tRow.real_value;
                }
                //add that row to the daily report row list
                dailyReportData.Add(lineRow);

                dailyId++;
            }
            dailyId = 1;
            //MessageBox.Show("completed");
            //return;
            //string lineName = lineNameCmb_st2.SelectedItem.ToString();
            try
            {
                //List<FinalTransactionBlockModel> lineReportData = await _consoleHandler.print_sta2(lineNameCmb_st2.SelectedItem.ToString());
                /*                foreach (var transaction in lineReportData)
                                {
                                    System.Diagnostics.Debug.WriteLine($"ID: {transaction.Id}, Line Name: {transaction.linename}, Transport Agent: {transaction.transportagent}, Company: {transaction.company}");
                                }*/
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Initialize totals to zero
                    int totalBagCount = 0, totalBoxCount = 0, totalLeafWeight = 0;
                    int totalWater = 0, totalMorapuwata = 0, totalThambimata = 0,
                        totalReject = 0, totalBagWeight = 0, totalBoxWeight = 0, totalDalu = 0;
                    // Only calculate totals if there's data
                    if (dailyReportData.Any())
                    {
                        foreach (var transaction in dailyReportData)
                        {
                            totalBoxCount += transaction.box_count; // boxFix: Box count fix added
                            totalBagCount += transaction.bag_count;
                            totalLeafWeight += transaction.total_leaf_weight + transaction.box_weight;
                            totalWater += transaction.water;
                            totalMorapuwata += transaction.morapuwata;
                            totalThambimata += transaction.thambimata;
                            totalReject += transaction.reject;
                            totalBagWeight += transaction.bag_weight;
                            totalBoxWeight += transaction.box_weight; // Use the box weight
                            totalDalu += transaction.total_leaf_weight - (transaction.water + transaction.morapuwata
                                         + transaction.thambimata + transaction.reject + transaction.bag_weight);
                        }
                    }

                    // Pagination setup - ensure at least 1 page even for empty data
                    int pageSize = 30;
                    int totalPages = dailyReportData.Count == 0 ? 1 : (int)Math.Ceiling((double)dailyReportData.Count / pageSize);
                    FixedDocument fixedDoc = new FixedDocument();
                    fixedDoc.DocumentPaginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                    for (int page = 0; page < totalPages; page++)
                    {
                        var pageData = dailyReportData.Count == 0
                            ? new List<DailyReportRowBlockModel>()  // Empty page
                            : dailyReportData
                                .Skip(page * pageSize)
                                .Take(pageSize)
                                .ToList();

                        FixedPage fixedPage = new FixedPage();
                        Canvas canvas = CreateDailyReportPage(
                            basedDate: reportDate.ToString(),
                            pageData: pageData,
                            pageNumber: page + 1,
                            totalPages: totalPages,
                            isLastPage: page == totalPages - 1,
                            totals: new TotalRow(
                                0, //this is used in line report only
                                totalBagCount, totalBoxCount, totalLeafWeight,
                                totalWater, totalMorapuwata, totalThambimata,
                                totalReject, totalBagWeight, totalBoxWeight, totalDalu
                            )
                        );

                        fixedPage.Children.Add(canvas);
                        PageContent pageContent = new PageContent();
                        pageContent.Child = fixedPage;
                        fixedDoc.Pages.Add(pageContent);
                    }

                    printDialog.PrintDocument(fixedDoc.DocumentPaginator, "Multi-Page Report");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Report error: " + ex.Message);
            }
        }

        private Canvas CreateDailyReportPage(string basedDate, List<DailyReportRowBlockModel> pageData, int pageNumber,
    int totalPages, bool isLastPage, TotalRow totals)
        {
            Canvas canvas = new Canvas { Width = 816, Height = 1056 }; // Standard letter size
            double yPos = 50;
            Typeface typeface = new Typeface("Arial");
            double fontSize = 13;
            double headerFontSize = 10;
            Brush brush = Brushes.Black;

            // Add main headers
            AddText(canvas, "සීමාසහිත මොරවක්කොරළේ තේ නිපදවන්නන්ගේ සමුපකාර සමිතිය", 14, 816 / 2, yPos, true);
            yPos += 30;
            AddText(canvas, _appConfig.branchName, 14, 816 / 2, yPos, true); //මොරවක්කෝරලේ තේ කම්හල || නව ඇලන්වැලි තේ කම්හල || කෝප්කෝලා තේ කම්හල //BRANCHCHANGE
            yPos += 30;
            AddText(canvas, "දෛනික වාර්තාව" + "(" + basedDate + ")", 14, 816 / 2, yPos, true);
            yPos += 30;

            // Report metadata
            AddText(canvas, $"Report Issue Index: IDX-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                fontSize, 50, yPos);
            yPos += 20;
            AddText(canvas, $"Date Issued: {DateTime.Now:yyyy.MM.dd}", fontSize, 50, yPos);
            yPos += 40;

            // Table header
            string[] headers = { "අං.", "ප්‍රවා. මාර්ගය", "ගෝනි(n)", "පෙට්ටි(n)", "මුළු බර", "වතුරට", "මෝරපුවට", "තැමිණීමට", "ප්‍රතික්ෂේපිත", "ගෝනි(KG)", "පෙට්ටි(KG)", "දළු(KG)" };
            double[] headerPositions = { 46, 70, 220, 270, 315, 360, 430, 490, 570, 650, 710, 760 };

            // Draw header background
            AddRectangle(canvas, 40, yPos - 5, 816 - 80, 30, Brushes.White);
            for (int i = 0; i < headers.Length; i++)
            {
                bool isNumericColumn = i >= 2; // Columns from index 2 are numeric
                AddText(canvas, headers[i], headerFontSize, headerPositions[i], yPos,
                    rightAlign: isNumericColumn, brush: Brushes.Black);
            }
            yPos += 25;

            // Data rows
            bool isAlternate = false;
            foreach (var transaction in pageData)
            {
                //if (isAlternate)
                //{
                //    AddRectangle(canvas, 40, yPos - 2, 816 - 80, 20, Brushes.LightGray);
                //}
                AddRectangle(canvas, 40, yPos - 2, 816 - 80, 20, Brushes.White);
                AddDailyTransactionRow(canvas, transaction, headerPositions, yPos, fontSize);
                yPos += 20;
                isAlternate = !isAlternate; // Alternate row color
            }
            AddSignatureLine(canvas, "Authorised by (Supervisor)", 50, yPos + 120);
            AddSignatureLine(canvas, "Authorised by (Leaf Weighting Officer)", 816 - 350, yPos + 120);
            // Totals and signatures (last page only)
            if (isLastPage)
            {
                // Totals row  { 220, 270, 315, 360, 430, 490, 570, 650, 710, 760 };
                AddRectangle(canvas, 40, yPos - 2, 816 - 80, 20, Brushes.White);
                AddText(canvas, totals.BagCount.ToString(), fontSize, 220, yPos, rightAlign: true);
                AddText(canvas, totals.BoxCount.ToString(), fontSize, 270, yPos, rightAlign: true);
                AddText(canvas, totals.LeafWeight.ToString(), fontSize, 315, yPos, rightAlign: true);
                AddText(canvas, totals.Water.ToString(), fontSize, 360, yPos, rightAlign: true);
                AddText(canvas, totals.Morapuwata.ToString(), fontSize, 430, yPos, rightAlign: true);
                AddText(canvas, totals.Thambimata.ToString(), fontSize, 490, yPos, rightAlign: true);
                AddText(canvas, totals.Reject.ToString(), fontSize, 570, yPos, rightAlign: true);
                AddText(canvas, totals.BagWeight.ToString(), fontSize, 650, yPos, rightAlign: true);
                AddText(canvas, totals.BoxWeight.ToString(), fontSize, 710, yPos, rightAlign: true);
                AddText(canvas, totals.Dalu.ToString(), fontSize, 760, yPos, rightAlign: true);

                // Signatures
                //AddSignatureLine(canvas, "Authorised by (Supervisor)", 50, yPos + 60);
                //AddSignatureLine(canvas, "Authorised by (Leaf Weighting Officer)", 816 - 350, yPos + 60);
            }

            // Page number
            AddText(canvas, $"Page {pageNumber} of {totalPages}", fontSize, 700, 1000);

            return canvas;
        }

        //weight scaler utils
        int ToCustomInt(double value)
        {
            // 1) Get the integer part (e.g. Floor(2.3) == 2)
            int integerPart = (int)Math.Floor(value);                              // :contentReference[oaicite:0]{index=0}

            // 2) Isolate the first decimal digit:
            //    Multiply by 10 and truncate to get e.g. Truncate(2.3 * 10) == 23
            //    Then mod 10 yields 3 (the tenths digit)
            int tenthsDigit = (int)(Math.Truncate(value * 10) % 10);               // :contentReference[oaicite:1]{index=1}

            // 3) Apply your rule:
            //    If tenths digit > 1, bump up; else stay at integerPart
            return tenthsDigit > 1
                ? integerPart + 1
                : integerPart;
        }




        /*private void DrawDailyReportPage(DrawingContext dc, List<FinalTransactionBlockModel> lineReportData)
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
                //test
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

        }*/
    }
}

