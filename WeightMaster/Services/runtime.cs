using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WeightMaster.Services
{
    internal class Runtime
    {
        private string filePath = @"C:\Users\Public\scalerEngine\weight_value.json";
        private FileSystemWatcher fileWatcher;
        private DispatcherTimer readTimer;
        private MainWindow window;
        private bool soundTrigger = false;
        private bool soundTriggerMax = false;
        private DateTime lastDataReceived = DateTime.MinValue;
        private bool isScaleConnected = false;
        public Runtime(MainWindow win, string file)
        {
            this.window = win;
            this.filePath = file;
        }

        public void SetFilePath(string path)
        {
            filePath = path;
        }


        public void ExecuteRunExe()
        {
            // Assuming 'run.exe' is in the same directory as the current application
            string exeFileName = "run.exe"; // No need for a full path

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", $"/C \"{exeFileName}\"")
                {
                    CreateNoWindow = true,  // Optionally hide the command prompt window
                    UseShellExecute = true
                };

                Process process = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing run.exe: {ex.Message}");
            }
        }


        public void KillRunExe()
        {
            string processName = "run"; // Name of the process without the ".exe"

            try
            {
                // Find all processes with the specified name
                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Length > 0)
                {
                    foreach (Process process in processes)
                    {
                        process.Kill(); // Kill the process
                        process.WaitForExit(); // Optional: wait for the process to fully exit
                        System.Diagnostics.Debug.WriteLine($"Successfully killed {processName}.exe");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{processName}.exe is not running.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error killing {processName}.exe: {ex.Message}");
            }
        }


        public void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
                StartFileWatcher();
                StartTimer();
            }
        }
        public void StartFileWatcher()
        {
            // Get the directory of the currently running .exe
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Set the file path to the weight_value.json located in the same directory
            SetFilePath(Path.Combine(exeDirectory, "weight_value.json"));

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"Error: File not found at {filePath}");
                UpdateScaleConnectionStatus(false);
                return;  // Exit if the file doesn't exist
            }

            try
            {
                // Dispose of the previous FileSystemWatcher if it exists
                fileWatcher?.Dispose();

                fileWatcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(filePath),
                    Filter = Path.GetFileName(filePath),
                    NotifyFilter = NotifyFilters.LastWrite
                };

                // Subscribe to the Changed event
                fileWatcher.Changed += OnFileChanged;
                fileWatcher.EnableRaisingEvents = true;

                // Perform an initial read of the file
                ReadFile();
            }
            catch (Exception ex)
            {
                // Handle any exceptions related to FileSystemWatcher
                System.Diagnostics.Debug.WriteLine($"Error initializing FileSystemWatcher: {ex.Message}");
            }
        }



        public void StartTimer()
        {
            if (readTimer == null)
            {
                readTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                readTimer.Tick += (s, e) =>
                {
                    ReadFile();
                    CheckConnectionTimeout();
                };
            }
            readTimer.Start();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(ReadFile);
        }

        private void ReadFile()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("Attempting to open file...");

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // System.Diagnostics.Debug.WriteLine("File opened successfully.");

                    using (var reader = new StreamReader(stream))
                    {
                        // System.Diagnostics.Debug.WriteLine("Reading file...");

                        string json = reader.ReadToEnd();
                        // System.Diagnostics.Debug.WriteLine("File content read successfully.");

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var data = JsonSerializer.Deserialize<WeightData>(json, options);

                        if (data != null)
                        {
                            // System.Diagnostics.Debug.WriteLine($"> Value: {data.Value}, Stable: {data.Stable}");
                            window.weightScalerValTxt_st1.Text = data.Value.ToUpper().Replace("KG", "").Trim();
                             window.weightScalerValTxt_st2.Text = data.Value.ToUpper().Replace("KG", "").Trim();

                            // Update scale connection status
                            lastDataReceived = DateTime.Now;
                            UpdateScaleConnectionStatus(true);

                            if (data.Stable.Equals("true"))
                            {
                                if (!window.weightScalerValTxt_st1.Text.ToString().Equals("0.0"))
                                {
                                    if (!soundTriggerMax)
                                    {

                                        soundTriggerMax = true;
                                    }
                                }
                                else
                                {
                                    soundTriggerMax = false;
                                }

                                    window.weightScalerStatus_st1.Foreground = new SolidColorBrush(Colors.Green);
                                window.weightScalerStatus_st1.Text = "සමබරයි";
                                window.weightScalerStatus_st2.Foreground = new SolidColorBrush(Colors.Green);
                                window.weightScalerStatus_st2.Text = "සමබරයි";
                                if (window.weightScalerValTxt_st1.Text.ToString().Equals("0.0")){
                                    if (!soundTrigger) {
                                        ConsoleSound.PlayReset();
                                        soundTrigger = true;
                                    }
                                }
                                else
                                {
                                    soundTrigger = false;
                                }


                            }
                            else
                            {
                                window.weightScalerStatus_st1.Foreground = new SolidColorBrush(Colors.Red);
                                window.weightScalerStatus_st1.Text = "අසමබරයි";
                                window.weightScalerStatus_st2.Foreground = new SolidColorBrush(Colors.Red);
                                window.weightScalerStatus_st2.Text = "අසමබරයි";
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Deserialized data is null.");
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                System.Diagnostics.Debug.WriteLine($"IOException occurred: {ioEx.Message}");

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Threading.Thread.Sleep(100);
                    ReadFile(); // Retry reading the file
                }, DispatcherPriority.Background);

                System.Diagnostics.Debug.WriteLine("Retrying file read after IO exception...");
            }
            catch (UnauthorizedAccessException unauthEx)
            {
                System.Diagnostics.Debug.WriteLine($"UnauthorizedAccessException occurred: {unauthEx.Message}");
                MessageBox.Show($"Error: You do not have access to read this file: {unauthEx.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General exception occurred: {ex.Message}");
                MessageBox.Show($"Error reading file: {ex.Message}");
            }
        }


        public class WeightData
        {
            public string Value { get; set; }
            public string Stable { get; set; }
        }

        public void OnWindowClosed()
        {
            fileWatcher?.Dispose();
            readTimer?.Stop();
        }

        /// <summary>
        /// Updates the scale connection status indicator on both stations.
        /// </summary>
        private void UpdateScaleConnectionStatus(bool connected)
        {
            if (isScaleConnected == connected) return; // No change
            isScaleConnected = connected;

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var color = connected
                        ? new SolidColorBrush(Color.FromRgb(46, 204, 113))  // Green
                        : new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red

                    var textColor = connected
                        ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                        : new SolidColorBrush(Color.FromRgb(153, 153, 153));

                    var statusText = connected ? "Connected" : "Disconnected";

                    // Update Station 1 indicator
                    window.ScaleStatusIndicator_st1.Fill = color;
                    window.ScaleStatusText_st1.Text = statusText;
                    window.ScaleStatusText_st1.Foreground = textColor;

                    // Update Station 2 indicator
                    window.ScaleStatusIndicator_st2.Fill = color;
                    window.ScaleStatusText_st2.Text = statusText;
                    window.ScaleStatusText_st2.Foreground = textColor;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating scale status: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the scale connection has timed out (no data for 5 seconds).
        /// Call this periodically to detect disconnection.
        /// </summary>
        public void CheckConnectionTimeout()
        {
            if (lastDataReceived != DateTime.MinValue &&
                (DateTime.Now - lastDataReceived).TotalSeconds > 5)
            {
                UpdateScaleConnectionStatus(false);
            }
        }
    }
}
